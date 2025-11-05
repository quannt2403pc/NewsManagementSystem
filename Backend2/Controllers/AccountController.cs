// File: Backend2/Controllers/AccountController.cs

using Backend2.Models;
using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;      // <-- 2. Add Claims using
using System.Text.Json;            // <-- 3. Add Json using
using System.Threading.Tasks;      // <-- 4. Add Task using
using System.Linq;
using Backend2.Services;                 // <-- 5. Add Linq using

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Only Admin can manage accounts
    public class AccountController : ControllerBase
    {
        private readonly ISystemAccountRepository _accountRepository;
        private readonly IAuditLogService _auditLogService; // <-- 6. Declare Service

        // 7. Inject Service in Constructor
        public AccountController(ISystemAccountRepository accountRepository, IAuditLogService auditLogService)
        {
            _accountRepository = accountRepository;
            _auditLogService = auditLogService; // <-- Assign Service
        }

        // --- UPDATE GetAccounts ---
        [HttpGet]
        public ActionResult<IEnumerable<AccountDto>> GetAccounts([FromQuery] string? search, [FromQuery] int? role)
        {
            var accounts = _accountRepository.GetAccounts(search, role);

            // Map to DTO to exclude password and potentially other sensitive fields
            var accountDtos = accounts.Select(a => new AccountDto
            {
                AccountId = a.AccountId,
                AccountEmail = a.AccountEmail,
                AccountName = a.AccountName, // Use correct field name
                AccountRole = a.AccountRole
            }).ToList();

            return Ok(accountDtos);
        }

        // --- UPDATE GetAccountById ---
        [HttpGet("{id}")]
        public ActionResult<AccountDto> GetAccountById(int id)
        {
            var account = _accountRepository.GetAccountById(id);
            if (account == null)
            {
                return NotFound();
            }

            // Map to DTO
            var accountDto = new AccountDto
            {
                AccountId = account.AccountId,
                AccountEmail = account.AccountEmail,
                AccountName = account.AccountName,
                AccountRole = account.AccountRole
            };

            return Ok(accountDto);
        }

        // --- UPDATE CreateAccount ---
        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] AccountCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (_accountRepository.IsEmailExist(dto.AccountEmail))
            {
                return BadRequest(new { message = "Email already exists." });
            }

            // !!! SECURITY WARNING: Storing plain text password !!!
            // You should HASH the password here before saving
            // Example using a hypothetical hashing service:
            // var hashedPassword = _passwordHasher.HashPassword(dto.AccountPassword);
            var newAccount = new SystemAccount
            {
                AccountEmail = dto.AccountEmail,
                AccountPassword = dto.AccountPassword, // Storing plain text - VERY BAD
                AccountName = dto.AccountName,
                AccountRole = dto.AccountRole
            };

            try
            {
                _accountRepository.AddAccount(newAccount);
                // newAccount.AccountId should be populated now

                // Add Audit Log (Exclude password from log)
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                var loggedData = new { newAccount.AccountId, newAccount.AccountEmail, newAccount.AccountName, newAccount.AccountRole };
                await _auditLogService.LogAsync(
                    userEmail, "Create", "SystemAccount",
                    JsonSerializer.Serialize(new { newAccount.AccountId }),
                    null,
                    loggedData // Log the DTO-like object without password
                );

                // Map to AccountDto for the response (don't return password)
                var createdAccountDto = new AccountDto { /* ... map fields ... */ };
                return CreatedAtAction(nameof(GetAccountById), new { id = newAccount.AccountId }, createdAccountDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error creating account: {ex.Message}" });
            }
        }

        // --- UPDATE UpdateAccount ---
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] AccountUpdateDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingAccount = _accountRepository.GetAccountById(id);
            if (existingAccount == null)
            {
                return NotFound(new { message = "Account not found." });
            }

            // Check email uniqueness (excluding self)
            if (_accountRepository.IsEmailExist(dto.AccountEmail, id))
            {
                return BadRequest(new { message = "Email already exists." });
            }

            // Snapshot for logging (exclude password)
            var oldValuesForLog = new { existingAccount.AccountId, existingAccount.AccountEmail, existingAccount.AccountName, existingAccount.AccountRole };

            try
            {
                // Map updated fields (DO NOT update password here)
                existingAccount.AccountEmail = dto.AccountEmail;
                existingAccount.AccountName = dto.AccountName;
                existingAccount.AccountRole = dto.AccountRole;

                _accountRepository.UpdateAccount(existingAccount);

                // Add Audit Log (exclude password)
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                var newValuesForLog = new { existingAccount.AccountId, existingAccount.AccountEmail, existingAccount.AccountName, existingAccount.AccountRole };
                await _auditLogService.LogAsync(
                    userEmail, "Update", "SystemAccount",
                    JsonSerializer.Serialize(new { AccountId = id }),
                    oldValuesForLog,
                    newValuesForLog
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error updating account: {ex.Message}" });
            }
        }

        // --- ChangePassword (Keep as is, but add Audit Log) ---
        [HttpPut("change-password/{id}")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordViewModel model)
        {
            var accountToUpdate = _accountRepository.GetAccountById(id);
            if (accountToUpdate == null)
            {
                return NotFound(new { message = "Account not found." });
            }

            // !!! SECURITY WARNING: Comparing plain text passwords !!!
            // You should verify the hash of model.CurrentPassword against the stored hash
            if (accountToUpdate.AccountPassword != model.CurrentPassword)
            {
                return BadRequest(new { message = "Current password incorrect." });
            }

            // Basic validation for new password
            if (string.IsNullOrWhiteSpace(model.NewPassword) || model.NewPassword.Length < 6)
            {
                return BadRequest(new { message = "New password must be at least 6 characters." });
            }

            try
            {
                // !!! SECURITY WARNING: Storing plain text password !!!
                // You should HASH the model.NewPassword before saving
                accountToUpdate.AccountPassword = model.NewPassword;
                _accountRepository.UpdateAccount(accountToUpdate);

                // Add Audit Log (Log the action, but NOT the passwords)
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                await _auditLogService.LogAsync(
                    userEmail, "ChangePassword", "SystemAccount",
                    JsonSerializer.Serialize(new { AccountId = id }),
                    new { ChangeType = "Password Updated" }, // Old values - indicate password change
                    new { ChangeType = "Password Updated" }  // New values - indicate password change
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error changing password: {ex.Message}" });
            }
        }

        // --- UPDATE DeleteAccount ---
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAccount(int id)
        {
            var accountToDelete = _accountRepository.GetAccountById(id);
            if (accountToDelete == null)
            {
                return NotFound(new { message = "Account not found." });
            }

            // Check if account created articles (keep this rule)
            if (_accountRepository.HasCreatedNewsArticles(id))
            {
                return BadRequest(new { message = "Cannot delete this account as it has created news articles." });
            }

            // Snapshot for logging (exclude password)
            var oldValuesForLog = new { accountToDelete.AccountId, accountToDelete.AccountEmail, accountToDelete.AccountName, accountToDelete.AccountRole };

            try
            {
                _accountRepository.DeleteAccount(id);

                // Add Audit Log
                var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "Unknown";
                await _auditLogService.LogAsync(
                    userEmail, "Delete", "SystemAccount",
                     JsonSerializer.Serialize(new { AccountId = id }),
                    oldValuesForLog,
                    null
                );

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error deleting account: {ex.Message}" });
            }
        }
    }
}