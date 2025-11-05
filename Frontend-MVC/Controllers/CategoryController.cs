using Frontend_MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers; 
using System.Net.Http.Json; 
using System.Text.Json; 
using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace Frontend_MVC.Controllers
{

    public class CategoryController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public CategoryController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        public async Task<IActionResult> Index([FromQuery] string? searchString)
        {
            var viewModel = new CategoryListViewModel
            {
                SearchString = searchString
            };

            var client = _httpClientFactory.CreateClient();

            // Lấy token nếu cần xác thực
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            // Gọi API Backend: GET /api/category/with-count?search={searchString}
            var apiUrl = $"{_apiBaseUrl}/api/Category/with-count";
            if (!string.IsNullOrEmpty(searchString))
            {
                apiUrl += $"?search={Uri.EscapeDataString(searchString)}";
            }

            try
            {
                var response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Đọc và deserialize JSON response
                    viewModel.Categories = await response.Content.ReadFromJsonAsync<List<CategoryWithArticleCount>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<CategoryWithArticleCount>();
                    ViewBag.ParentCategories = viewModel.Categories.ToList();
                }
                else
                {
                    // Xử lý lỗi (ví dụ: hiển thị thông báo)
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ViewBag.Error = $"Không thể tải danh mục. Status: {response.StatusCode}. Details: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi kết nối API: {ex.Message}";
            }

            return View(viewModel); // Trả về View Index.cshtml với dữ liệu
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CategoryCreateUpdateViewModel model)
        {
            // Backend yêu cầu JSON, nên ta nhận [FromBody]
            if (!ModelState.IsValid)
            {
                // Trả về lỗi validation dưới dạng JSON để JavaScript xử lý
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "Phiên đăng nhập hết hạn." });
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // API Backend: POST /api/category
            var apiUrl = $"{_apiBaseUrl}/api/Category";

            try
            {
                // Chuẩn bị dữ liệu JSON để gửi đi
                // Chỉ gửi những trường mà Backend cần (không gửi AvailableParentCategories)
                var categoryData = new
                {
                    // CategoryId không cần gửi khi tạo mới
                    model.CategoryName,
                    model.CategoryDescription,
                    model.ParentCategoryId, // Gửi null nếu không chọn
                    model.IsActive
                };
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(categoryData),
                    Encoding.UTF8,
                    "application/json");

                // Gọi API Backend
                var response = await client.PostAsync(apiUrl, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    // Thành công (Backend trả về 201 Created)
                    return Ok(new { success = true });
                }
                else
                {
                    // Lỗi từ Backend (400 Bad Request - tên trùng, thiếu field...)
                    var errorBody = await response.Content.ReadAsStringAsync();
                    string errorMessage = $"Lỗi từ API ({response.StatusCode})";
                    try
                    {
                        var errorDoc = JsonDocument.Parse(errorBody);
                        // Backend trả về { message: "..." }
                        if (errorDoc.RootElement.TryGetProperty("message", out var msg))
                        {
                            errorMessage = msg.GetString() ?? errorMessage;
                        }
                        // Hoặc có thể là lỗi validation của ASP.NET Core
                        else if (errorDoc.RootElement.TryGetProperty("errors", out _))
                        {
                            errorMessage = "Dữ liệu không hợp lệ."; // Thông báo chung
                        }
                    }
                    catch { /* Bỏ qua nếu không parse được JSON */ }
                    return StatusCode((int)response.StatusCode, new { success = false, message = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetCategory(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            // Only add auth header if token exists AND API requires it for GET
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var apiUrl = $"{_apiBaseUrl}/api/Category/{id}";

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    // Backend returns the full Category model, map it to our ViewModel
                    // We can reuse CategoryWithArticleCount as it has the needed fields
                    var categoryData = await response.Content.ReadFromJsonAsync<CategoryWithArticleCount>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return Ok(categoryData); // Return JSON data to JavaScript
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to load category data." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"API connection error: {ex.Message}" });
            }
        }


        [HttpPost] // Using POST from JS for simplicity, but it calls PUT on Backend
        public async Task<IActionResult> Update([FromBody] CategoryCreateUpdateViewModel model)
        {
            if (model.CategoryId == 0) // Basic check
            {
                ModelState.AddModelError("CategoryId", "Category ID is missing.");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "Session expired." });
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Backend API endpoint: PUT /api/category/{id}
            var apiUrl = $"{_apiBaseUrl}/api/Category/{model.CategoryId}";

            try
            {
                // Prepare JSON data (Backend expects the Category model structure)
                var categoryData = new
                {
                    model.CategoryId, // Must include ID for Backend PUT validation
                    model.CategoryName,
                    model.CategoryDescription,
                    model.ParentCategoryId,
                    model.IsActive
                    // Add any other fields Backend's Category model expects for update
                };
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(categoryData),
                    Encoding.UTF8,
                    "application/json");

                // Call Backend API using PUT method
                var response = await client.PutAsync(apiUrl, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    // Success (Backend returns 204 No Content)
                    return Ok(new { success = true });
                }
                else
                {
                    // Handle Backend errors (400 Bad Request, 404 Not Found, etc.)
                    var errorBody = await response.Content.ReadAsStringAsync();
                    string errorMessage = $"API Error ({response.StatusCode})";
                    try
                    {
                        var errorDoc = JsonDocument.Parse(errorBody);
                        if (errorDoc.RootElement.TryGetProperty("message", out var msg))
                        {
                            errorMessage = msg.GetString() ?? errorMessage;
                        }
                    }
                    catch { /* Ignore if not JSON */ }
                    return StatusCode((int)response.StatusCode, new { success = false, message = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"System error: {ex.Message}" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int id) // Receives ID via form data or query string
        {
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid Category ID." });
            }

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { success = false, message = "Session expired." });
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Backend API endpoint: DELETE /api/category/{id}
            var apiUrl = $"{_apiBaseUrl}/api/Category/{id}";

            try
            {
                // Call Backend API using DELETE method
                var response = await client.DeleteAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // Success (Backend returns 204 No Content)
                    return Ok(new { success = true });
                }
                else
                {
                    // Handle Backend errors (400 Bad Request - used by articles, 404 Not Found, etc.)
                    var errorBody = await response.Content.ReadAsStringAsync();
                    string errorMessage = $"API Error ({response.StatusCode})";
                    try
                    {
                        var errorDoc = JsonDocument.Parse(errorBody);
                        if (errorDoc.RootElement.TryGetProperty("message", out var msg))
                        {
                            errorMessage = msg.GetString() ?? errorMessage;
                        }
                    }
                    catch { /* Ignore if not JSON */ }
                    return StatusCode((int)response.StatusCode, new { success = false, message = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"System error: {ex.Message}" });
            }
        }



    }
}