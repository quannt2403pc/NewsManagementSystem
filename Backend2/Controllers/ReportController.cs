using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class ReportController : ControllerBase
    {
        private readonly IReportRepository _reportRepository;

        public ReportController(IReportRepository reportRepository)
        {
            _reportRepository = reportRepository;
        }

        [HttpGet]
        public ActionResult<IEnumerable<ArticleReport>> GetReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string groupBy = "category")
        {
            var report = _reportRepository.GetArticlesReport(startDate, endDate, groupBy);
            return Ok(report);
        }

        [HttpGet("totals")]
        public ActionResult<ArticleTotalReport> GetTotals(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var totals = _reportRepository.GetArticleTotals(startDate, endDate);
            return Ok(totals);
        }

    }
}
