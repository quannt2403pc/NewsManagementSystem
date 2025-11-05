using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt; // <-- THÊM DÒNG NÀY
using System.Security.Claims;          // <-- THÊM DÒNG NÀY
using System.Linq;
using Frontend_MVC.ViewModels;
using System.Net.Http.Headers;
using System.Text;                     // <-- THÊM DÒNG NÀY

namespace Frontend_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        // [GET] /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // [POST] /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var client = _httpClientFactory.CreateClient();
            var jsonContent = JsonContent.Create(model);
            var apiUrl = $"{_apiBaseUrl}/api/Login";

            try
            {
                var response = await client.PostAsync(apiUrl, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonSerializer.Deserialize<LoginResponseViewModel>(responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
                    {
                        // LƯU TOKEN VÀO SESSION
                        HttpContext.Session.SetString("JWToken", tokenResponse.Token);

                        // ===================================================
                        // PHẦN CẬP NHẬT: ĐỌC TOKEN VÀ CHUYỂN HƯỚNG THEO ROLE
                        // ===================================================

                        // 1. Khởi tạo handler để đọc token
                        var handler = new JwtSecurityTokenHandler();
                        var jwtToken = handler.ReadJwtToken(tokenResponse.Token);

                        // 2. Tìm claim chứa thông tin Role
                        // (Backend của bạn đang dùng ClaimTypes.Role)
                        var roleClaim = jwtToken.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role);

                        if (roleClaim == null)
                        {
                            ModelState.AddModelError(string.Empty, "Token không chứa thông tin vai trò (role).");
                            return View(model);
                        }

                        string userRole = roleClaim.Value;

                        // 3. Lưu role vào Session (Tùy chọn, nhưng rất hữu ích)
                        HttpContext.Session.SetString("UserRole", userRole);

                        // 4. Chuyển hướng dựa trên role
                        if (userRole == "Admin")
                        {
                            // Chuyển đến AdminController, action Index
                            return RedirectToAction("Index", "Admin");
                        }
                        else if (userRole == "Staff" || userRole == "Lecturer")
                        {
                            // Chuyển đến StaffController, action Index
                            return RedirectToAction("Index", "Staff");
                        }
                        else
                        {
                            // Nếu có role khác mà bạn không xử lý, về trang chủ
                            return RedirectToAction("Index", "Home");
                        }
                        // ===================================================
                        // KẾT THÚC PHẦN CẬP NHẬT
                        // ===================================================
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Không nhận được token từ API.");
                        return View(model);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Đăng nhập thất bại: {errorContent}");
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Không thể kết nối đến máy chủ: {ex.Message}");
                return View(model);
            }
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JWToken");
            HttpContext.Session.Remove("UserRole"); // <-- Cũng nên xóa role khi logout
            return RedirectToAction("Login", "Account");
        }

        public async Task<IActionResult> Index([FromQuery] string? searchString, [FromQuery] int? role)
        {
            var viewModel = new AccountListViewModel
            {
                SearchString = searchString,
                SelectedRole = role
            };

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                // Handle case where user is not logged in or token expired
                // return RedirectToAction("Login", "Auth"); // Example redirect
                TempData["Error"] = "Session expired. Please log in again.";
                // Or allow viewing if API allows anonymous GET?
            }

            // Build API URL with query parameters
            var query = new List<string>();
            if (!string.IsNullOrEmpty(searchString)) query.Add($"search={Uri.EscapeDataString(searchString)}");
            if (role.HasValue) query.Add($"role={role.Value}");
            string queryString = query.Any() ? "?" + string.Join("&", query) : "";
            var apiUrl = $"{_apiBaseUrl}/api/Account{queryString}";

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    viewModel.Accounts = await response.Content.ReadFromJsonAsync<List<AccountViewModel>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<AccountViewModel>();
                }
                else
                {
                    ViewBag.Error = $"Error loading accounts: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"API Connection Error: {ex.Message}";
            }

            return View(viewModel);
        }

        // GET: /Account/GetAccount/5 (For Edit Modal)
        [HttpGet]
        public async Task<IActionResult> GetAccount(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return Unauthorized(); // Must be logged in
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_apiBaseUrl}/api/Account/{id}";
            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    // Backend returns AccountDto, map to AccountUpdateViewModel for the form
                    var accountDto = await response.Content.ReadFromJsonAsync<AccountViewModel>(
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (accountDto == null) return NotFound();

                    var updateViewModel = new AccountUpdateViewModel
                    {
                        AccountId = accountDto.AccountId,
                        AccountEmail = accountDto.AccountEmail,
                        AccountName = accountDto.AccountName,
                        AccountRole = accountDto.AccountRole
                    };
                    return Ok(updateViewModel); // Return JSON for JavaScript
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to load account." });
                }
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // POST: /Account/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AccountCreateViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return Unauthorized(new { message = "Session expired." });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_apiBaseUrl}/api/Account";
            try
            {
                // Send necessary fields for AccountCreateDto
                var accountData = new { model.AccountEmail, model.AccountPassword, model.AccountName, model.AccountRole };
                var jsonContent = new StringContent(JsonSerializer.Serialize(accountData), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, jsonContent);
                return await HandleApiResponse(response); // Use helper function
            }
            catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
        }

        // POST: /Account/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] AccountUpdateViewModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return Unauthorized(new { message = "Session expired." });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_apiBaseUrl}/api/Account/{model.AccountId}";
            try
            {
                // Send necessary fields for AccountUpdateDto
                var accountData = new { model.AccountEmail, model.AccountName, model.AccountRole };
                var jsonContent = new StringContent(JsonSerializer.Serialize(accountData), Encoding.UTF8, "application/json");

                var response = await client.PutAsync(apiUrl, jsonContent);
                return await HandleApiResponse(response); // Use helper function
            }
            catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordViewModel1 model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return Unauthorized(new { message = "Session expired." });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_apiBaseUrl}/api/Account/change-password/{model.AccountId}";
            try
            {
                // Send necessary fields for Backend's ChangePasswordViewModel
                var passwordData = new { model.CurrentPassword, model.NewPassword };
                var jsonContent = new StringContent(JsonSerializer.Serialize(passwordData), Encoding.UTF8, "application/json");

                var response = await client.PutAsync(apiUrl, jsonContent);
                return await HandleApiResponse(response); // Use helper function
            }
            catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
        }

        // POST: /Account/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid ID." });

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token)) return Unauthorized(new { message = "Session expired." });
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_apiBaseUrl}/api/Account/{id}";
            try
            {
                var response = await client.DeleteAsync(apiUrl);
                return await HandleApiResponse(response); // Use helper function
            }
            catch (Exception ex) { return StatusCode(500, new { success = false, message = ex.Message }); }
        }

        // --- Helper Function for API Responses ---
        private async Task<IActionResult> HandleApiResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                // Handle 201 Created, 204 No Content, 200 OK with success body
                if (response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    // Optionally read the created object if needed
                    // var createdObject = await response.Content.ReadFromJsonAsync<object>();
                    // return Ok(new { success = true, data = createdObject });
                }
                return Ok(new { success = true });
            }
            else
            {
                string errorMessage = $"API Error ({response.StatusCode})";
                try
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    using var jsonDoc = JsonDocument.Parse(errorBody);
                    if (jsonDoc.RootElement.TryGetProperty("message", out var msg))
                    {
                        errorMessage = msg.GetString() ?? errorMessage;
                    }
                    else if (jsonDoc.RootElement.TryGetProperty("title", out var title)) // ASP.NET Core validation errors
                    {
                        errorMessage = title.GetString() ?? errorMessage;
                        // Potentially extract more details from "errors" property if needed
                    }
                }
                catch { /* Ignore if body is not JSON */ }
                return StatusCode((int)response.StatusCode, new { success = false, message = errorMessage });
            }
        }















    }
}