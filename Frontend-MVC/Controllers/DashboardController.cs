using Frontend_MVC.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text; // Cho StringBuilder

namespace Frontend_MVC.Controllers
{
    // [Authorize(Roles = "Admin")] // Đảm bảo chỉ Admin mới vào được
    public class DashboardController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl; // Core API
        private readonly string _analyticsApiBaseUrl; // Analytics API
        private readonly IWebHostEnvironment _env;

        public DashboardController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
            _analyticsApiBaseUrl = configuration.GetValue<string>("AnalyticsApiBaseUrl"); // Lấy URL Analytics API
        }

        // GET: /Dashboard/Index
        public async Task<IActionResult> Index(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int? categoryId,
            [FromQuery] bool? status)
        {
            var viewModel = new DashboardViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                CategoryId = categoryId,
                Status = status
            };

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken"); // Lấy token
            if (string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login", "Auth"); // Chuyển hướng nếu chưa đăng nhập
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); // Gắn token nếu Analytics API yêu cầu

            try
            {
                // --- 1. Tải dữ liệu bộ lọc (từ Core API) ---
                viewModel.AvailableCategories = await GetCategoriesAsync(client);

                // --- 2. Tải dữ liệu Dashboard (từ Analytics API) ---
                var dashboardApiUrl = BuildFilteredUrl("/api/analytics/dashboard", viewModel);
                var dashboardResponse = await client.GetAsync(dashboardApiUrl);

                if (dashboardResponse.IsSuccessStatusCode)
                {
                    viewModel.DashboardData = await dashboardResponse.Content.ReadFromJsonAsync<DashboardDataDto>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new DashboardDataDto();

                    Console.WriteLine($"Dashboard data: {JsonSerializer.Serialize(viewModel.DashboardData)}");
                }
                else
                {
                    ViewBag.Error = $"Lỗi tải dữ liệu dashboard: {dashboardResponse.ReasonPhrase}";
                    viewModel.DashboardData = new DashboardDataDto(); // Khởi tạo để tránh lỗi null
                }

                // --- 3. Tải dữ liệu Trending (từ Analytics API) ---
                var trendingApiUrl = $"{_analyticsApiBaseUrl}/api/analytics/trending";
                var trendingResponse = await client.GetAsync(trendingApiUrl);

                if (trendingResponse.IsSuccessStatusCode)
                {
                    viewModel.TrendingArticles = await trendingResponse.Content.ReadFromJsonAsync<List<TrendingArticleDto>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<TrendingArticleDto>();
                }
                else
                {
                    ViewBag.Error += " Lỗi tải trending articles.";
                    viewModel.TrendingArticles = new List<TrendingArticleDto>();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Lỗi kết nối API: {ex.Message}";
                viewModel.DashboardData = new DashboardDataDto();
                viewModel.TrendingArticles = new List<TrendingArticleDto>();
            }

            return View(viewModel);
        }

        // GET: /Dashboard/ExportExcel
        public async Task<IActionResult> ExportExcel(
            [FromQuery] string? startDate,
            [FromQuery] string? endDate,
            [FromQuery] int? categoryId,
            [FromQuery] bool? status)
        {
            var filters = new DashboardViewModel { StartDate = startDate, EndDate = endDate, CategoryId = categoryId, Status = status };
            var apiUrl = BuildFilteredUrl("/api/analytics/export", filters);

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            // ... (gắn token nếu cần) ...

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var fileStream = await response.Content.ReadAsStreamAsync();
                    string fileName = $"Export_BaoCao_BaiViet_{DateTime.Now:yyyyMMdd}.xlsx";
                    return File(fileStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
                }
                else
                {
                    TempData["Error"] = "Không thể xuất file Excel. Lỗi API.";
                    return RedirectToAction("Index", filters);
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Lỗi kết nối khi xuất file: {ex.Message}";
                return RedirectToAction("Index", filters);
            }
        }

        // --- Helper: Lấy Categories từ Core API ---
        private async Task<List<CategoryViewModel>> GetCategoriesAsync(HttpClient client)
        {
            var apiUrl = $"{_apiBaseUrl}/api/Category";
            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<CategoryViewModel>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<CategoryViewModel>();
                }
            }
            catch (Exception ex) { Console.WriteLine($"Error fetching categories: {ex.Message}"); }
            return new List<CategoryViewModel>();
        }

        // --- Helper: Xây dựng URL với bộ lọc ---
        private string BuildFilteredUrl(string relativePath, DashboardViewModel filters)
        {
            var queryString = new StringBuilder();
            if (!string.IsNullOrEmpty(filters.StartDate)) queryString.Append($"&startDate={filters.StartDate}");
            if (!string.IsNullOrEmpty(filters.EndDate)) queryString.Append($"&endDate={filters.EndDate}");
            if (filters.CategoryId.HasValue) queryString.Append($"&categoryId={filters.CategoryId.Value}");
            if (filters.Status.HasValue) queryString.Append($"&status={filters.Status.Value}");

            // Thay ? đầu tiên nếu có
            if (queryString.Length > 0) queryString[0] = '?';

            return $"{_analyticsApiBaseUrl}{relativePath}{queryString.ToString()}";
        }
    }
}