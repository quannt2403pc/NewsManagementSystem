using AiApi.Services;
using AiApi.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AiApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AiController : ControllerBase
    {
        private readonly IAiService _aiService;

        public AiController(IAiService aiService)
        {
            _aiService = aiService;
        }

        /// <summary>
        /// Gợi ý các từ khóa (tags) từ nội dung văn bản.
        /// </summary>
        /// <param name="request">Đối tượng JSON chứa nội dung: { "content": "..." }</param>
        /// <returns>Một danh sách các chuỗi (tags) gợi ý.</returns>
        // POST: /api/ai/suggest-tags
        [HttpPost("suggest-tags")]
        public async Task<IActionResult> SuggestTags([FromBody] SuggestTagRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { message = "Nội dung bài viết không được để trống." });
            }

            try
            {
                // Giới hạn nội dung (ví dụ: 1000 từ) để tiết kiệm và tăng tốc
                var truncatedContent = string.Join(" ", request.Content.Split(' ').Take(1000));

                var tags = await _aiService.SuggestTagsAsync(truncatedContent);

                // Trả về một mảng JSON: ["tag1", "tag2", "tag3"]
                return Ok(tags);
            }
            catch (Exception ex)
            {
                // Ghi lại lỗi (log) ở đây
                return StatusCode(500, new { message = $"Lỗi máy chủ nội bộ: {ex.Message}" });
            }
        }
    }
}
