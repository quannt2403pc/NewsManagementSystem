using Backend2.Models;
using Backend2.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsArticleController : ControllerBase
    {
        private readonly INewsArticleRepository _newsArticleRepository;

        public NewsArticleController(INewsArticleRepository newsArticleRepository)
        {
            _newsArticleRepository = newsArticleRepository;
        }

        [HttpGet("active")]
        public ActionResult<IEnumerable<NewsArticle>> GetActiveNews()
        {
            try
            {
                var news = _newsArticleRepository.GetActiveNewsArticles();
                if(news == null || !news.Any())
                {
                    return NotFound("No news found");
                }
                Console.WriteLine(news.Count());
                return Ok(news);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("details/{id}")]
        [AllowAnonymous] // Cho phép truy cập công khai
        public ActionResult<NewsArticle> GetArticleDetails(int id)
        {
            var article = _newsArticleRepository.GetNewsArticleDetails(id);

            if (article == null || article.NewsStatus != true)
            {
                return NotFound("Article not found or not active.");
            }

            return Ok(article);
        }


    }
}
