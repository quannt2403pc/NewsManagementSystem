// File: Frontend-MVC/Controllers/TagController.cs
using Frontend_MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Authorization; // If needed

namespace Frontend_MVC.Controllers
{
    // [Authorize(Roles = "Admin, Staff")] // Add authorization if required
    public class TagController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public TagController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        // GET: /Tag/Index or /Tag
        public async Task<IActionResult> Index([FromQuery] string? searchString)
        {
            var viewModel = new TagListViewModel
            {
                SearchString = searchString
            };

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            var apiUrl = $"{_apiBaseUrl}/api/Tag?search={Uri.EscapeDataString(searchString ?? "")}";

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    viewModel.Tags = await response.Content.ReadFromJsonAsync<List<TagViewModel>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<TagViewModel>();
                }
                else
                {
                    ViewBag.Error = $"Error loading tags: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"API Connection Error: {ex.Message}";
            }

            return View(viewModel);
        }

        // GET: /Tag/GetTag/5 (For Edit Modal)
        [HttpGet]
        public async Task<IActionResult> GetTag(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            var apiUrl = $"{_apiBaseUrl}/api/Tag/{id}";

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var tagData = await response.Content.ReadFromJsonAsync<TagViewModel>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return Ok(tagData);
                }
                else
                {
                    return StatusCode((int)response.StatusCode, new { message = "Failed to load tag data." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"API connection error: {ex.Message}" });
            }
        }

        // POST: /Tag/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TagCreateUpdateViewModel model)
        {
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

            var apiUrl = $"{_apiBaseUrl}/api/Tag";
            try
            {
                // Only send TagName for create
                var tagData = new { model.TagName, model.Note };
                var jsonContent = new StringContent(JsonSerializer.Serialize(tagData), Encoding.UTF8, "application/json");

                var response = await client.PostAsync(apiUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true });
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    string msg = GetErrorMessageFromJson(errorBody, response.StatusCode);
                    return StatusCode((int)response.StatusCode, new { success = false, message = msg });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"System error: {ex.Message}" });
            }
        }

        // POST: /Tag/Update
        [HttpPost]
        public async Task<IActionResult> Update([FromBody] TagCreateUpdateViewModel model)
        {
            if (model.TagId <= 0) ModelState.AddModelError("TagId", "Tag ID is missing.");
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

            var apiUrl = $"{_apiBaseUrl}/api/Tag/{model.TagId}";
            try
            {
                // Send TagName for update
                var tagData = new { model.TagName, model.TagId , model.Note}; // Include TagId if backend PUT needs it for validation
                var jsonContent = new StringContent(JsonSerializer.Serialize(tagData), Encoding.UTF8, "application/json");

                var response = await client.PutAsync(apiUrl, jsonContent);
                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true });
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    string msg = GetErrorMessageFromJson(errorBody, response.StatusCode);
                    return StatusCode((int)response.StatusCode, new { success = false, message = msg });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"System error: {ex.Message}" });
            }
        }

        // POST: /Tag/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid Tag ID." });

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "Session expired." });
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_apiBaseUrl}/api/Tag/{id}";
            try
            {
                var response = await client.DeleteAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true });
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    string msg = GetErrorMessageFromJson(errorBody, response.StatusCode);
                    return StatusCode((int)response.StatusCode, new { success = false, message = msg });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"System error: {ex.Message}" });
            }
        }

        // Helper to extract error message
        private string GetErrorMessageFromJson(string jsonBody, System.Net.HttpStatusCode statusCode)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(jsonBody);
                if (jsonDoc.RootElement.TryGetProperty("message", out var msg))
                {
                    return msg.GetString() ?? $"API Error ({statusCode})";
                }
                if (jsonDoc.RootElement.TryGetProperty("title", out var title)) // For validation errors
                {
                    return title.GetString() ?? $"API Error ({statusCode})";
                }
            }
            catch { /* Ignore if not JSON */ }
            return $"API Error ({statusCode})";
        }
    }
}