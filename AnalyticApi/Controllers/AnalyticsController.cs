using AnalyticApi.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticRepository _analyticsRepo;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(IAnalyticRepository analyticsRepo, ILogger<AnalyticsController> logger)
        {
            _analyticsRepo = analyticsRepo;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardData(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? categoryId,
            [FromQuery] bool? status)
        {
            try
            {
                var data = await _analyticsRepo.GetDashboardDataAsync(startDate, endDate, categoryId, status);
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy dữ liệu dashboard.");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("trending")]
        public async Task<IActionResult> GetTrendingArticles()
        {
            try
            {
                var data = await _analyticsRepo.GetTrendingArticlesAsync(5); // Lấy top 5
                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy trending articles.");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportToExcel(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? categoryId,
            [FromQuery] bool? status)
        {
            try
            {
                var fileBytes = await _analyticsRepo.ExportDashboardDataAsync(startDate, endDate, categoryId, status);

                string fileName = $"BaoCao_BaiViet_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất file Excel.");
                return StatusCode(500, new { message = ex.Message });
            }
        }

  
        
    }
}
