using Backend2.Models;
using Backend2.Repositories.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicNewsController : ControllerBase
    {
        private readonly IPublicNewsRepository _publicNewsRepository;

        public PublicNewsController(IPublicNewsRepository publicNewsRepository)
        {
            _publicNewsRepository = publicNewsRepository;
        }

        [HttpGet("search")]
        public ActionResult<IEnumerable<NewsArticle>> Search(
            [FromQuery] string? search,
            [FromQuery] string? categoryName,
            [FromQuery] string? tagName,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var articles = _publicNewsRepository.SearchPublicNews(
                search,
                categoryName,
                tagName,
                startDate,
                endDate
            );

            if (articles == null || !articles.Any())
            {
                return NotFound("Không tìm thấy bài báo nào phù hợp.");
            }

            return Ok(articles);
        }
        
    }
}

