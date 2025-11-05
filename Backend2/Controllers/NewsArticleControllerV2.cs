using Backend2.Hubs;
using Backend2.Models;
using Backend2.Repositories.Class;
using Backend2.Repositories.Interface;
using Backend2.Services;
using Backend2.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsArticleV2Controller : ControllerBase
    {
        private readonly INewsArticleRepositoryV2 _newsRepository;
        private readonly INewsArticleRepository _newsArticleRepository;
        private readonly ISystemAccountRepository _accountRepository;
        private readonly IAuditLogService _auditLogService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly JsonSerializerOptions _auditJsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = false
        };
        public NewsArticleV2Controller(INewsArticleRepositoryV2 newsRepository, ISystemAccountRepository accountRepository, 
            INewsArticleRepository newsArticleRepository, 
            IWebHostEnvironment webHostEnvironment,
            IAuditLogService auditLogService,
            IHubContext<NotificationHub> hubContext)
        {
            _newsRepository = newsRepository;
            _accountRepository = accountRepository;
            _newsArticleRepository = newsArticleRepository;
            _webHostEnvironment = webHostEnvironment;   
            _webHostEnvironment = webHostEnvironment;   
            _auditLogService = auditLogService;
            _hubContext = hubContext;
        }


        // API: GET /api/news
        [HttpGet]
        public async Task<IActionResult> GetNews(
            [FromQuery] string? searchString,
            [FromQuery] string? sortBy = "CreatedDate",
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var result = await _newsRepository.GetNewsArticlesAsync(searchString, sortBy, sortDirection, pageNumber, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


   
        [HttpPost]
        public async Task<IActionResult> CreateArticle( 
            [FromForm] NewsArticleDto request) 
        {
            if (request == null)
                return BadRequest("Invalid request");

            // Xác thực người dùng (giữ nguyên)
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _accountRepository.GetAccountByEmail(userEmail);
            if (user == null) return Unauthorized();

            string? coverImagePath = null; // Biến để lưu đường dẫn ảnh

            // Logic xử lý file upload (giữ nguyên)
            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                try
                {
                    // Đảm bảo thư mục wwwroot/images/news tồn tại
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "news");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    // Tạo tên file duy nhất
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetExtension(request.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Lưu file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.ImageFile.CopyToAsync(fileStream);
                    }

                    // Đường dẫn tương đối để lưu vào DB (ví dụ: /images/news/ten_file.jpg)
                    coverImagePath = Path.Combine("/images/news", uniqueFileName).Replace("\\", "/");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = $"Lỗi khi lưu file: {ex.Message}" });
                }
            }

            // Map từ DTO (NewsArticleDto) sang Model (NewsArticle)
            var newsArticle = new NewsArticle
            {
                NewsTitle = request.NewsTitle,
                Headline = request.Headline,
                NewsContent = request.NewsContent,
                NewsSource = request.NewsSource,
                CategoryId = request.CategoryId,
                NewsStatus = request.NewsStatus ?? true,
                CreatedById = user.AccountId,
                CreatedDate = DateTime.Now,

                // <-- SỬA 2: Dùng đúng tên trường 'ImageUrl' trong Model của bạn
                ImageUrl = coverImagePath
            };

            try
            {
                // Gọi Repository (giữ nguyên)
                _newsArticleRepository.AddNewsArticle(newsArticle, request.TagIds);

                try
                {
                    string notificationMessage = $"Bài viết mới đã được tạo: '{newsArticle.NewsTitle}'";
                    string? articleDetailUrl = $"https://localhost:7022/newsarticle/detail/{newsArticle.NewsArticleId}";
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        message = notificationMessage,
                        articleUrl = articleDetailUrl
                    });
                    Console.WriteLine($"[SignalR] Sent notification: {notificationMessage}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SignalR] Error sending notification: {ex.Message}");
                }






                await _auditLogService.LogAsync(
                userEmail,
                "Create",
                "NewsArticle",
                JsonSerializer.Serialize(new { newsArticle.NewsArticleId }),
                null, // Không có giá trị cũ
                newsArticle // Giá trị mới
            );
                return Ok(new { success = true, message = "Tạo bài viết thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi lưu vào CSDL: {ex.Message}" });
            }
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult GetArticleById(int id)
        {
            try
            {
                // 1. SỬ DỤNG HÀM MỚI CỦA BẠN
                var newsArticle = _newsArticleRepository.GetNewsArticleDetails(id);

                if (newsArticle == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy bài viết" });
                }

                // 2. Map sang DTO (bao gồm cả thông tin cập nhật)
                var dto = new DetailNewsArticleDto
                {
                    NewsArticleID = newsArticle.NewsArticleId,
                    NewsTitle = newsArticle.NewsTitle,
                    Headline = newsArticle.Headline,
                    NewsContent = newsArticle.NewsContent,
                    CreatedDate = newsArticle.CreatedDate,
                    ImageUrl = newsArticle.ImageUrl,

                    CategoryName = newsArticle.Category?.CategoryName,
                    AuthorName = newsArticle.CreatedBy?.AccountName, // Giả sử dùng Email, bạn có thể đổi thành FullName nếu có
                    TagNames = newsArticle.Tags.Select(t => t.TagName).ToList(),

                    // THÊM DỮ LIỆU TỪ Include(na => na.UpdatedBy)
                    ModifiedDate = newsArticle.ModifiedDate,
                    UpdatedByName = newsArticle.UpdatedBy?.AccountName // Tên người cập nhật
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }




      
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateArticle(int id, [FromForm] NewsArticleDto request)
        {
            if (request == null)
                return BadRequest("Invalid request");

            // 1. Xác thực người dùng
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _accountRepository.GetAccountByEmail(userEmail);
            if (user == null) return Unauthorized();

            // 2. Lấy bài viết CŨ từ DB (Repo phải có hàm này)
            var existingArticle = _newsArticleRepository.GetNewsArticleById(id);
            var oldValuesJson = JsonSerializer.Serialize(existingArticle, _auditJsonOptions);
            var oldValuesObject = JsonSerializer.Deserialize<NewsArticle>(oldValuesJson, _auditJsonOptions);
            if (existingArticle == null)
            {
                return NotFound(new { success = false, message = "Bài viết không tồn tại." });
            }

            // 3. Xử lý file ảnh (Logic của Update)
            string? coverImagePath = existingArticle.ImageUrl; // Giữ lại ảnh cũ làm mặc định

            if (request.ImageFile != null && request.ImageFile.Length > 0)
            {
                // Nếu có file MỚI, thì xử lý giống hệt Create
                try
                {
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "news");
                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetExtension(request.ImageFile.FileName);
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await request.ImageFile.CopyToAsync(fileStream);
                    }

                    coverImagePath = Path.Combine("/images/news", uniqueFileName).Replace("\\", "/");

                    // (Xóa file ảnh CŨ nếu có)
                    if (!string.IsNullOrEmpty(existingArticle.ImageUrl))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, existingArticle.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { success = false, message = $"Lỗi khi lưu file mới: {ex.Message}" });
                }
            }

            // 4. Map DTO sang Model (cập nhật 'existingArticle')
            // Đây là Model sẽ được gửi xuống Repository
            existingArticle.NewsTitle = request.NewsTitle;
            existingArticle.Headline = request.Headline;
            existingArticle.NewsContent = request.NewsContent;
            existingArticle.NewsSource = request.NewsSource;
            existingArticle.CategoryId = request.CategoryId;
            existingArticle.NewsStatus = request.NewsStatus ?? true;
            existingArticle.UpdatedById = user.AccountId; // Cập nhật người sửa
            existingArticle.ModifiedDate = DateTime.Now; // Cập nhật ngày sửa
            existingArticle.ImageUrl = coverImagePath; // Gán đường dẫn ảnh (mới hoặc cũ)

            // 5. Gọi Repository (ĐÚNG CHỮ KÝ)
            try
            {
                // Tham số 1: Model NewsArticle đã được cập nhật
                // Tham số 2: Danh sách TagIds mới
                _newsArticleRepository.UpdateNewsArticle(existingArticle, request.TagIds);
                await _auditLogService.LogAsync(
                userEmail,
                "Update",
                "NewsArticle",
                JsonSerializer.Serialize(new { existingArticle.NewsArticleId }),
                oldValuesObject, // Giá trị cũ
                existingArticle // Giá trị mới
            );
                return Ok(new { success = true, message = "Cập nhật bài viết thành công" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Lỗi khi cập nhật CSDL: {ex.Message}" });
            }
        }



        [HttpDelete("{id}")]
        public IActionResult DeleteArticle(int id)
        {
            // 1. Xác thực người dùng
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _accountRepository.GetAccountByEmail(userEmail);
            if (user == null) return Unauthorized();

            try
            {
                // 2. Lấy bài viết (để có đường dẫn ảnh)
                // (Giả định repo GetNewsArticleById không include Tags cũng được)
                var newsArticle = _newsArticleRepository.GetNewsArticleById(id);
                if (newsArticle == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy bài viết để xóa" });
                }
                
                string? imageUrl = newsArticle.ImageUrl;

                _newsArticleRepository.DeleteNewsArticle(id);
                _auditLogService.LogAsync( 
                userEmail,
                "Delete",
                "NewsArticle",
                JsonSerializer.Serialize(new { newsArticle.NewsArticleId }),
                newsArticle, 
                null 
            ).Wait();
                if (!string.IsNullOrEmpty(imageUrl))
                {
                    var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                return Ok(new { success = true, message = "Xóa bài viết thành công" });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi nếu việc xóa thất bại (ví dụ: lỗi ràng buộc khóa ngoại)
                return StatusCode(500, new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }






    }
}
