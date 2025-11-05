using Frontend_MVC.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Frontend_MVC.Controllers
{
    public class AuditLogController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;

        public AuditLogController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
        }

        // GET: /AuditLog/Index
        public async Task<IActionResult> Index(
            [FromQuery] string? userEmail,
            [FromQuery] string? entityName,
            [FromQuery] int pageNumber = 1)
        {
            if (pageNumber < 1) pageNumber = 1;

            var viewModel = new AuditLogListViewModel
            {
                UserEmail = userEmail,
                EntityName = entityName,
                CurrentPage = pageNumber
            };

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth"); // Cần trang đăng nhập
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Xây dựng Query String
            var query = new StringBuilder($"?pageNumber={pageNumber}&pageSize=15");
            if (!string.IsNullOrEmpty(userEmail))
            {
                query.Append($"&userEmail={Uri.EscapeDataString(userEmail)}");
            }
            if (!string.IsNullOrEmpty(entityName))
            {
                query.Append($"&entityName={Uri.EscapeDataString(entityName)}");
            }

            var apiUrl = $"{_apiBaseUrl}/api/auditlog{query.ToString()}";

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    viewModel.LogResponse = await response.Content.ReadFromJsonAsync<PaginationResponseViewModel<AuditLogDto>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new PaginationResponseViewModel<AuditLogDto> { Items = new List<AuditLogDto>() };
                }
                else
                {
                    ViewBag.Error = $"Không thể tải log: {response.ReasonPhrase}";
                    viewModel.LogResponse = new PaginationResponseViewModel<AuditLogDto> { Items = new List<AuditLogDto>() };
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi kết nối API: {ex.Message}";
                viewModel.LogResponse = new PaginationResponseViewModel<AuditLogDto> { Items = new List<AuditLogDto>() };
            }

            return View(viewModel);
        }
    }
}