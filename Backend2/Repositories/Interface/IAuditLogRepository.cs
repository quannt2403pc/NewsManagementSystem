using Backend2.Models;
using Backend2.ViewModels;

namespace Backend2.Repositories.Interface
{
    public interface IAuditLogRepository
    {
        Task<PaginationResponse<AuditLog>> GetAuditLogsAsync(
            string? userEmail,
            string? entityName,
            int pageNumber,
            int pageSize);
    }
}
