using Backend2.Repositories.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Backend2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Chỉ Admin mới được xem Log
    public class AuditLogController : ControllerBase
    {
        private readonly IAuditLogRepository _auditLogRepo;

        public AuditLogController(IAuditLogRepository auditLogRepo)
        {
            _auditLogRepo = auditLogRepo;
        }

        // GET: /api/auditlog?userEmail=...&entityName=...&pageNumber=...
        [HttpGet]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? userEmail,
            [FromQuery] string? entityName,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 15) // Lấy 15 mục mỗi trang
        {
            try
            {
                var result = await _auditLogRepo.GetAuditLogsAsync(userEmail, entityName, pageNumber, pageSize);
                return Ok(result);
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new { message = $"Lỗi máy chủ: {ex.Message}" });
            }
        }
    }
}

