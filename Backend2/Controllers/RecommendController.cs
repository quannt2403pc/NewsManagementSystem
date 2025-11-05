using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecommendController : ControllerBase
    {
        private readonly INewsArticleRepository _newsArticleRepository;

        public RecommendController(INewsArticleRepository newsArticleRepository)
        {
            _newsArticleRepository = newsArticleRepository;
        }
        [HttpGet("{id}")]
        public IActionResult GetRelatedArticles(int id)
        {
            try
            {
                var relatedArticles = _newsArticleRepository.GetRelatedArticles(id);

                var dtoList = relatedArticles.Select(article => new RelatedArticleDto
                {
                    NewsArticleID = article.NewsArticleId,
                    NewsTitle = article.NewsTitle,
                    Headline = article.Headline
                }).ToList();

                return Ok(dtoList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }





    }
}
