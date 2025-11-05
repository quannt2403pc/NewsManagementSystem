using Frontend_MVC.ViewModels; 
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations; 
using Microsoft.AspNetCore.Http;

namespace Frontend_MVC.Controllers
{
    public class NewsArticleController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly string _aiApiBaseUrl;
        private readonly IWebHostEnvironment _env;
        public NewsArticleController(IHttpClientFactory httpClientFactory, IConfiguration configuration, IWebHostEnvironment env)
        {
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = configuration.GetValue<string>("ApiBaseUrl");
            _aiApiBaseUrl = configuration.GetValue<string>("AiApiBaseUrl");
            _env = env;
        }
        private string GetOfflineFilePath()
        {
            // Ensure the 'json' directory exists in wwwroot
            string folderPath = Path.Combine(_env.WebRootPath, "json");
            Directory.CreateDirectory(folderPath); // Creates the directory if it doesn't exist
            return Path.Combine(folderPath, "newsArticle.json");
        }


        // GET: /NewsArticle/Index
        public async Task<IActionResult> Index(
                [FromQuery] string? searchString,
                [FromQuery] string? sortBy = "CreatedDate",
                [FromQuery] string? sortDirection = "desc",
                [FromQuery] int pageNumber = 1)
        {
            if (pageNumber < 1) pageNumber = 1;

            var client = _httpClientFactory.CreateClient();
            var queryString = new StringBuilder($"?pageNumber={pageNumber}&pageSize=10&sortBy={sortBy}&sortDirection={sortDirection}");
            if (!string.IsNullOrEmpty(searchString))
            {
                queryString.Append($"&searchString={Uri.EscapeDataString(searchString)}");
            }
            var apiUrl = $"{_apiBaseUrl}/api/NewsArticleV2{queryString.ToString()}";

            var viewModel = new NewsListViewModel
            {
                CurrentPage = pageNumber,
                SearchString = searchString,
                SortBy = sortBy,
                SortDirection = sortDirection
            };

            bool isOffline = false; // Flag for offline status
            string jsonFilePath = GetOfflineFilePath();

            try
            {
                // ** Attempt to call the API **
                HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.IsSuccessStatusCode)
                {
                    // ** ONLINE: API Success **
                    isOffline = false;
                    ViewBag.IsOffline = false; // Pass status to View

                    // Read data from API response stream
                    var responseStream = await response.Content.ReadAsStreamAsync();
                    viewModel.NewsResponse = await JsonSerializer.DeserializeAsync<PaginationResponseViewModel<NewsArticleViewModel>>(
                        responseStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                    );
                    viewModel.NewsResponse ??= new PaginationResponseViewModel<NewsArticleViewModel> { Items = new List<NewsArticleViewModel>() }; // Ensure not null

                    // ** Save the successful response data to the JSON file **
                    try
                    {
                        // Serialize the NewsResponse object (contains items and pagination info)
                        string jsonToSave = JsonSerializer.Serialize(viewModel.NewsResponse, new JsonSerializerOptions { WriteIndented = true });
                        await System.IO.File.WriteAllTextAsync(jsonFilePath, jsonToSave);
                        Console.WriteLine($"[NewsArticleController] Saved API data to {jsonFilePath}");
                    }
                    catch (Exception fileEx)
                    {
                        Console.WriteLine($"[NewsArticleController] Error saving offline JSON: {fileEx.Message}");
                        ViewBag.Warning = "Could not save data for offline mode."; // Optional warning
                    }
                }
                else
                {
                    // ** OFFLINE: API Error (4xx, 5xx) **
                    isOffline = true;
                    ViewBag.IsOffline = true;
                    ViewBag.Error = $"API Error ({response.StatusCode}). Attempting to load local data.";
                    Console.WriteLine($"[NewsArticleController] API Error: {response.StatusCode}. Loading offline data.");
                    // Attempt to load from JSON file
                    viewModel.NewsResponse = await LoadOfflineDataAsync(jsonFilePath);
                }
            }
            catch (HttpRequestException httpEx) // Specific exception for network/connection issues
            {
                // ** OFFLINE: Connection Error **
                isOffline = true;
                ViewBag.IsOffline = true;
                ViewBag.Error = $"Connection Error: {httpEx.Message}. Attempting to load local data.";
                Console.WriteLine($"[NewsArticleController] Connection Error: {httpEx.Message}. Loading offline data.");
                // Attempt to load from JSON file
                viewModel.NewsResponse = await LoadOfflineDataAsync(jsonFilePath);
            }
            catch (Exception ex) // Catch other potential errors
            {
                // ** OFFLINE: Generic Error **
                isOffline = true;
                ViewBag.IsOffline = true;
                ViewBag.Error = $"An unexpected error occurred: {ex.Message}. Attempting to load local data.";
                Console.WriteLine($"[NewsArticleController] Unexpected Error: {ex.Message}. Loading offline data.");
                // Attempt to load from JSON file
                viewModel.NewsResponse = await LoadOfflineDataAsync(jsonFilePath);
            }

            // Ensure NewsResponse is never null for the View
            viewModel.NewsResponse ??= new PaginationResponseViewModel<NewsArticleViewModel> { Items = new List<NewsArticleViewModel>() };

            // Load Categories and Tags for Modal (handle potential errors if offline)
            try
            {
                ViewBag.CategoriesForModal = await GetCategoriesAsync();
                ViewBag.TagsForModal = await GetTagsAsync();
            }
            catch (Exception ex)
            {
                // If fetching dropdowns fails while offline, show an error but continue rendering the page
                ViewBag.DropdownError = $"Could not load categories/tags: {ex.Message}";
                ViewBag.CategoriesForModal = new List<CategoryViewModel>(); // Provide empty lists
                ViewBag.TagsForModal = new List<TagViewModel>();
            }


            return View(viewModel);
        }

        // --- Helper Function to Load Offline Data ---
        private async Task<PaginationResponseViewModel<NewsArticleViewModel>> LoadOfflineDataAsync(string filePath)
        {
            try
            {
                if (System.IO.File.Exists(filePath))
                {
                    string json = await System.IO.File.ReadAllTextAsync(filePath);
                    var data = JsonSerializer.Deserialize<PaginationResponseViewModel<NewsArticleViewModel>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    Console.WriteLine($"[NewsArticleController] Successfully loaded offline data from {filePath}");
                    return data ?? new PaginationResponseViewModel<NewsArticleViewModel> { Items = new List<NewsArticleViewModel>() };
                }
                else
                {
                    Console.WriteLine($"[NewsArticleController] Offline file not found: {filePath}");
                    ViewBag.Error += " Local data file not found."; // Append to existing error message
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NewsArticleController] Error loading offline JSON: {ex.Message}");
                ViewBag.Error += $" Error loading local data: {ex.Message}";
            }
            // Return default empty structure on any error during load
            return new PaginationResponseViewModel<NewsArticleViewModel> { Items = new List<NewsArticleViewModel>() };
        }

        // GET: /NewsArticle/Index (Hoặc /NewsArticle)
        // Hiển thị danh sách bài viết
    


        public async Task<IActionResult> Create()
        {
            var viewModel = new NewsArticleUpdateRequest // Sử dụng ViewModel bạn yêu cầu
            {
                Categories = await GetCategoriesAsync(),
                Tags = await GetTagsAsync()
            };
            return View(viewModel); // Trả về View ~/Views/NewsArticle/Create.cshtml
        }

      
        [HttpPost]

        public async Task<IActionResult> Create(NewsArticleUpdateRequest model) // Sử dụng ViewModel bạn yêu cầu
        {
            if (string.IsNullOrWhiteSpace(model.NewsTitle))
                ModelState.AddModelError(nameof(model.NewsTitle), "Tiêu đề là bắt buộc.");
            if (model.CategoryId == null)
                ModelState.AddModelError(nameof(model.CategoryId), "Vui lòng chọn danh mục.");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);

            }

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { success = false, message = "Phiên đăng nhập hết hạn." });

            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using (var content = new MultipartFormDataContent())
            {
                content.Add(new StringContent(model.NewsTitle ?? ""), nameof(model.NewsTitle));
                content.Add(new StringContent(model.Headline ?? ""), nameof(model.Headline));
                content.Add(new StringContent(model.NewsContent ?? ""), nameof(model.NewsContent));
                content.Add(new StringContent(model.NewsSource ?? ""), nameof(model.NewsSource));
                content.Add(new StringContent(model.CategoryId?.ToString() ?? ""), nameof(model.CategoryId));
                content.Add(new StringContent(model.NewsStatus?.ToString() ?? "true"), nameof(model.NewsStatus));
                foreach (var tagId in model.TagIds ?? new List<int>())
                {
                    content.Add(new StringContent(tagId.ToString()), nameof(model.TagIds));
                }
                if (model.ImageFile != null)
                {
                    var fileStreamContent = new StreamContent(model.ImageFile.OpenReadStream());
                    fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImageFile.ContentType);
                    content.Add(fileStreamContent, nameof(model.ImageFile), model.ImageFile.FileName);
                }

                var apiUrl = $"{_apiBaseUrl}/api/NewsArticleV2";
                try
                {
                    var response = await client.PostAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        return Ok(new { success = true });

                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        string errorMessage = $"Lỗi từ API ({response.StatusCode})";
                        try
                        {
                            var errorDoc = JsonDocument.Parse(errorBody);
                            if (errorDoc.RootElement.TryGetProperty("message", out var msg)) errorMessage = msg.GetString() ?? errorMessage;
                            else if (errorDoc.RootElement.TryGetProperty("title", out var title)) errorMessage = title.GetString() ?? errorMessage;
                        }
                        catch { }

                        return StatusCode((int)response.StatusCode, new { success = false, message = errorMessage });

                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });

                }
            }
        }



        [HttpPost]
        public async Task<IActionResult> Delete(int id) // Nhận 'id' từ FormData
        {
            if (id == 0)
            {
                return BadRequest(new { success = false, message = "Thiếu ID bài viết." });
            }

            // 1. Lấy token
            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { success = false, message = "Phiên đăng nhập hết hạn." });
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 2. Gọi Backend API với phương thức DELETE
            var apiUrl = $"{_apiBaseUrl}/api/NewsArticleV2/{id}";

            try
            {
                var response = await client.DeleteAsync(apiUrl); // <-- Dùng DeleteAsync

                if (response.IsSuccessStatusCode)
                {
                    return Ok(new { success = true }); // Trả về thành công cho AJAX
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    string errorMessage = $"Lỗi từ API ({response.StatusCode})";
                    try
                    {
                        var errorDoc = JsonDocument.Parse(errorBody);
                        if (errorDoc.RootElement.TryGetProperty("message", out var msg)) errorMessage = msg.GetString() ?? errorMessage;
                    }
                    catch { }
                    return StatusCode((int)response.StatusCode, new { success = false, message = errorMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }



        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var viewModel = new NewsArticleDetailViewModel();
            var client = _httpClientFactory.CreateClient();

            // (Xử lý xác thực nếu cần)
            var token = HttpContext.Session.GetString("JWToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            try
            {

                // Task 1: Gọi Core API (Chi tiết)
                var articleTask = client.GetAsync($"{_apiBaseUrl}/api/NewsArticleV2/{id}");

                var relatedTask = client.GetAsync($"{_apiBaseUrl}/api/recommend/{id}");

                await Task.WhenAll(articleTask, relatedTask);

                // Xử lý Task 1 (Giữ nguyên)
                var articleResponse = await articleTask;
                if (articleResponse.IsSuccessStatusCode)
                {
                    viewModel.Article = await articleResponse.Content.ReadFromJsonAsync<DetailNewsArticleDto>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (viewModel.Article != null && !string.IsNullOrEmpty(viewModel.Article.ImageUrl))
                    {
                        viewModel.Article.ImageUrl = $"{_apiBaseUrl.TrimEnd('/')}{viewModel.Article.ImageUrl}";

                    }
                }
                else
                {
                    TempData["Error"] = "Không tìm thấy bài viết.";
                    return RedirectToAction("Index");
                }

                // Xử lý Task 2 (Giữ nguyên)
                var relatedResponse = await relatedTask;
                if (relatedResponse.IsSuccessStatusCode)
                {
                    viewModel.RelatedArticles = await relatedResponse.Content.ReadFromJsonAsync<List<ArticleSuggestionDto>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<ArticleSuggestionDto>();
                }

            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Không thể tải dữ liệu chi tiết: {ex.Message}";
            }

            if (viewModel.Article == null)
            {
                TempData["Error"] = "Không thể tải bài viết.";
                return RedirectToAction("Index");
            }

            return View(viewModel);
        }



        // POST: /NewsArticle/SuggestTags
        // Action này được gọi bằng AJAX từ JavaScript trong Modal/View Create
        [HttpPost]
        public async Task<IActionResult> SuggestTags([FromBody] SuggestTagsApiRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Content))
                return BadRequest(new { message = "Nội dung không được để trống." });
            if (string.IsNullOrEmpty(_aiApiBaseUrl))
                return StatusCode(500, new { message = "AI API URL chưa được cấu hình." });

            var client = _httpClientFactory.CreateClient("AiApiClient"); // Có thể cần đăng ký client này trong Program.cs nếu chưa
            var apiUrl = $"{_aiApiBaseUrl.TrimEnd('/')}/api/ai/suggest-tags";

            try
            {
                var apiRequestData = new { content = request.Content };
                var jsonContent = new StringContent(JsonSerializer.Serialize(apiRequestData), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    var suggestedTags = await response.Content.ReadFromJsonAsync<List<string>>();
                    return Ok(suggestedTags ?? new List<string>()); // Trả về JSON array
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    string errorMessage = "Lỗi khi gọi AI API.";
                    try
                    {
                        var errorDoc = JsonDocument.Parse(errorBody);
                        if (errorDoc.RootElement.TryGetProperty("message", out var msg)) errorMessage = msg.GetString() ?? errorMessage;
                    }
                    catch { }
                    return StatusCode((int)response.StatusCode, new { message = errorMessage });
                }
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết ở đây
                return StatusCode(500, new { message = $"Lỗi hệ thống khi gọi AI: {ex.Message}" });
            }
        }

        // --- HÀM HỖ TRỢ (PRIVATE) ---

        // Lấy danh sách Categories từ Core API
        private async Task<List<CategoryViewModel>> GetCategoriesAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_apiBaseUrl}/api/Category";
            Console.WriteLine($"[GetCategoriesAsync] Calling API: {apiUrl}"); // Giữ lại log để kiểm tra
            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var categories = await response.Content.ReadFromJsonAsync<List<CategoryViewModel>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<CategoryViewModel>();
                    Console.WriteLine($"[GetCategoriesAsync] Success! Received {categories.Count} categories."); // Giữ lại log
                    return categories;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GetCategoriesAsync] API Error! Status: {response.StatusCode}, Response: {errorContent}"); // Giữ lại log
                    ViewBag.DropdownError = $"Lỗi tải danh mục: {response.ReasonPhrase}";
                    return new List<CategoryViewModel>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetCategoriesAsync] Exception! Message: {ex.Message}"); // Giữ lại log
                ViewBag.DropdownError = $"Lỗi kết nối khi tải danh mục: {ex.Message}";
                return new List<CategoryViewModel>();
            }
        }

        // Lấy danh sách Tags từ Core API
        private async Task<List<TagViewModel>> GetTagsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_apiBaseUrl}/api/Tag";
            Console.WriteLine($"[GetTagsAsync] Calling API: {apiUrl}"); // Giữ lại log
            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    var tags = await response.Content.ReadFromJsonAsync<List<TagViewModel>>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? new List<TagViewModel>();
                    Console.WriteLine($"[GetTagsAsync] Success! Received {tags.Count} tags."); // Giữ lại log
                    if (tags.Any(t => t == null || string.IsNullOrEmpty(t.TagName)))
                    {
                        Console.WriteLine("[GetTagsAsync] WARNING: Received list contains invalid tag data."); // Giữ lại log
                    }
                    return tags;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[GetTagsAsync] API Error! Status: {response.StatusCode}, Response: {errorContent}"); // Giữ lại log
                    ViewBag.DropdownError = $"Lỗi tải thẻ (tag): {response.ReasonPhrase}";
                    return new List<TagViewModel>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GetTagsAsync] Exception! Message: {ex.Message}"); // Giữ lại log
                ViewBag.DropdownError = $"Lỗi kết nối khi tải thẻ (tag): {ex.Message}";
                return new List<TagViewModel>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetNewsArticle(int id)
        {
            var client = _httpClientFactory.CreateClient();
            var apiUrl = $"{_apiBaseUrl}/api/NewsArticleV2/{id}";

            // Lấy token (giống Create)
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { message = "Phiên đăng nhập hết hạn." });
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                var response = await client.GetAsync(apiUrl);
                if (response.IsSuccessStatusCode)
                {
                    // Đọc về (giả sử bạn có NewsArticleDto trong Frontend ViewModels)
                    var articleDto = await response.Content.ReadFromJsonAsync<NewsArticleViewModel>(
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    return Ok(articleDto); // Trả JSON này về cho JavaScript
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Không thể tải dữ liệu bài viết.", details = error });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi kết nối: {ex.Message}" });
            }
        }


        // POST: /NewsArticle/Update
        // Action này nhận form submit từ modal (giống hệt Create)
        [HttpPost]
        public async Task<IActionResult> Update(NewsArticleUpdateRequest model) // Dùng chung NewsArticleUpdateRequest
        {
            // Kiểm tra ID
            if (model.NewsArticleId == 0)
            {
                return BadRequest(new { success = false, message = "Thiếu ID bài viết." });
            }

            // Validation (giống Create)
            if (string.IsNullOrWhiteSpace(model.NewsTitle))
                ModelState.AddModelError(nameof(model.NewsTitle), "Tiêu đề là bắt buộc.");
            if (model.CategoryId == null)
                ModelState.AddModelError(nameof(model.CategoryId), "Vui lòng chọn danh mục.");

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient();
            var token = HttpContext.Session.GetString("JWToken");
            if (string.IsNullOrEmpty(token))
            {
                return Unauthorized(new { success = false, message = "Phiên đăng nhập hết hạn." });
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Build MultipartFormDataContent (giống hệt Create)
            using (var content = new MultipartFormDataContent())
            {
                content.Add(new StringContent(model.NewsTitle ?? ""), nameof(model.NewsTitle));
                content.Add(new StringContent(model.Headline ?? ""), nameof(model.Headline));
                content.Add(new StringContent(model.NewsContent ?? ""), nameof(model.NewsContent));
                content.Add(new StringContent(model.NewsSource ?? ""), nameof(model.NewsSource));
                content.Add(new StringContent(model.CategoryId?.ToString() ?? ""), nameof(model.CategoryId));
                content.Add(new StringContent(model.NewsStatus?.ToString() ?? "true"), nameof(model.NewsStatus));
                foreach (var tagId in model.TagIds ?? new List<int>())
                {
                    content.Add(new StringContent(tagId.ToString()), nameof(model.TagIds));
                }

                if (model.ImageFile != null)
                {
                    var fileStreamContent = new StreamContent(model.ImageFile.OpenReadStream());
                    fileStreamContent.Headers.ContentType = new MediaTypeHeaderValue(model.ImageFile.ContentType);
                    content.Add(fileStreamContent, nameof(model.ImageFile), model.ImageFile.FileName);
                }

                // SỬA: API URL và phương thức
                var apiUrl = $"{_apiBaseUrl}/api/NewsArticleV2/{model.NewsArticleId}";
                try
                {
                    // SỬA: Dùng PutAsync
                    var response = await client.PutAsync(apiUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        return Ok(new { success = true });
                    }
                    else
                    {
                        var errorBody = await response.Content.ReadAsStringAsync();
                        string errorMessage = $"Lỗi từ API ({response.StatusCode})";
                        try
                        {
                            var errorDoc = JsonDocument.Parse(errorBody);
                            if (errorDoc.RootElement.TryGetProperty("message", out var msg)) errorMessage = msg.GetString() ?? errorMessage;
                        }
                        catch { }
                        return StatusCode((int)response.StatusCode, new { success = false, message = errorMessage });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
                }

            }
















        }

        [HttpPost]
        public IActionResult SaveOfflineData([FromBody] List<NewsArticleViewModel> data)
        {
            try
            {
                string folderPath = Path.Combine(_env.WebRootPath, "json");
                string filePath = Path.Combine(folderPath, "newsArticle.json");

                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                System.IO.File.WriteAllText(filePath, json);

                return Ok(new { message = "Offline data saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to save data: {ex.Message}" });
            }
        }

        // ✅ API để đọc dữ liệu offline
        [HttpGet]
        public IActionResult GetOfflineData()
        {
            try
            {
                string filePath = Path.Combine(_env.WebRootPath, "json", "newsArticle.json");
                if (!System.IO.File.Exists(filePath))
                    return Ok(new List<NewsArticleViewModel>()); // Nếu chưa có file, trả về mảng rỗng

                string json = System.IO.File.ReadAllText(filePath);
                var data = JsonSerializer.Deserialize<List<NewsArticleViewModel>>(json);
                return Ok(data ?? new List<NewsArticleViewModel>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to load offline data: {ex.Message}" });
            }
        }








        // Class nhỏ cho SuggestTags (có thể đã có ở file ViewModel riêng)
        public class SuggestTagsApiRequest {
            public string Content { get; set; } }

    }








}

