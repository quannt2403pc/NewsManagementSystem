using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ISystemAccountRepository _accountRepository;
        private readonly IConfiguration _configuration;

        public LoginController(ISystemAccountRepository accountRepository, IConfiguration configuration)
        {
            _accountRepository = accountRepository;
            _configuration = configuration;
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.Email) || string.IsNullOrEmpty(model.Password))
                {
                    return BadRequest("Email or Password must be filled");
                }

                var adminEmail = _configuration.GetValue<string>("AdminAccount:Email");
                var adminPassword = _configuration.GetValue<string>("AdminAccount:Password");

                // Kiểm tra admin
                if (model.Email == adminEmail && model.Password == adminPassword)
                {
                    var adminClaims = new[]
                    {

                new Claim(ClaimTypes.Email, adminEmail),
                new Claim(ClaimTypes.Role, "Admin")
            };
                    var adminToken = GenerateJwtToken(adminClaims);
                    return Ok(new { token = adminToken });
                }

                // Kiểm tra tài khoản thường
                var account = await _accountRepository.GetAccountByEmailAndPasswordAsync(model.Email, model.Password);

                if (account == null)
                {
                    return Unauthorized("Invalid email or password");
                }

                var claims = new[]
                {
            new Claim(ClaimTypes.Email, account.AccountEmail),
            new Claim(ClaimTypes.Role, account.AccountRole == 1 ? "Staff" : "Lecturer")
        };

                var token = GenerateJwtToken(claims);

                return Ok(new { token });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        private string GenerateJwtToken(Claim[] claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}

