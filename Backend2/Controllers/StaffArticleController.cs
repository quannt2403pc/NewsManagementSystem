using Backend2.Models;
using Backend2.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Staff")]
    public class StaffArticleController : ControllerBase
    {
        private readonly INewsArticleRepository _newsArticleRepository;
        private readonly ISystemAccountRepository _accountRepository;
        public StaffArticleController(INewsArticleRepository newsArticleRepository, ISystemAccountRepository accountRepository)
        {
            _newsArticleRepository = newsArticleRepository;
            _accountRepository = accountRepository;
        }

        [HttpGet("public")]
        [AllowAnonymous]
        public ActionResult<IEnumerable<NewsArticle>> GetPublicNews([FromQuery] string? search)
        {
            var articles = _newsArticleRepository.GetNewsArticles(search, status: true);
            return Ok(articles);
        }

        [HttpGet]
        public ActionResult<IEnumerable<NewsArticle>> GetArticles(
      [FromQuery] string? search,
      [FromQuery] string? authorName,
      [FromQuery] string? categoryName,
      [FromQuery] bool? status,
      [FromQuery] DateTime? startDate,
      [FromQuery] DateTime? endDate,
      [FromQuery] string? sort)
        {
            var articles = _newsArticleRepository.GetNewsArticles(search, authorName, categoryName, status, startDate, endDate, sort);
            return Ok(articles);
        }

        [HttpGet("{id}")]
        public ActionResult<NewsArticle> GetArticleById(int id)
        {
            var article = _newsArticleRepository.GetNewsArticleById(id);
            if (article == null)
            {
                return NotFound();
            }
            return Ok(article);
        }

        [HttpPost]
        public IActionResult CreateArticle([FromBody] NewsArticleUpdateRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request");

            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _accountRepository.GetAccountByEmail(userEmail);
            if (user == null) return Unauthorized();

            var newsArticle = new NewsArticle
            {
                NewsTitle = request.NewsTitle,
                Headline = request.Headline,
                NewsContent = request.NewsContent,
                NewsSource = request.NewsSource,
                CategoryId = request.CategoryId,
                NewsStatus = request.NewsStatus ?? true,
                CreatedById = user.AccountId,
                CreatedDate = DateTime.Now
            };

            _newsArticleRepository.AddNewsArticle(newsArticle, request.TagIds);

            return CreatedAtAction(nameof(GetArticleById), new { id = newsArticle.NewsArticleId }, newsArticle);
        }

        //[HttpPut("{id}")]
        //public IActionResult UpdateArticle(int id, [FromBody] NewsArticleUpdateRequest request)
        //{
        //    if (id != request.NewsArticleId)
        //        return BadRequest("Mismatched article ID");

        //    var userEmail = User.FindFirstValue(ClaimTypes.Email);
        //    var user = _accountRepository.GetAccountByEmail(userEmail);
        //    if (user == null) return Unauthorized();

        //    _newsArticleRepository.UpdateNewsArticle(request, user.AccountId);

        //    return NoContent();
        //}



        [HttpPost("duplicate/{id}")]
        public IActionResult DuplicateArticle(int id)
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            var user = _accountRepository.GetAccountByEmail(userEmail);

            var createdById = user.AccountId;
            _newsArticleRepository.DuplicateNewsArticle(id, createdById);
            return NoContent();
        }

        //[HttpDelete("{id}")]
        //public IActionResult DeleteArticle(int id)
        //{
        //    _newsArticleRepository.DeleteNewsArticle(id);
        //    return NoContent();
        //}



    }
}
