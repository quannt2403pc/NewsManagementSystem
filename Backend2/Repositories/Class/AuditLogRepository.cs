using Backend2.Models;
using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Backend2.Repositories.Class
{
    public class AuditLogRepository : IAuditLogRepository
    {
        Prn232Assignment1Context _context;

        public AuditLogRepository(Prn232Assignment1Context context)
        {
            _context = context;
        }
        public async Task<PaginationResponse<AuditLog>> GetAuditLogsAsync(
              string? userEmail, string? entityName, int pageNumber, int pageSize)
        {
            var query = _context.AuditLogs.AsQueryable();

            // 1. Lọc theo User (Email)
            if (!string.IsNullOrEmpty(userEmail))
            {
                query = query.Where(log => log.UserEmail != null && log.UserEmail.Contains(userEmail));
            }

            // 2. Lọc theo Entity Name
            if (!string.IsNullOrEmpty(entityName))
            {
                query = query.Where(log => log.EntityName == entityName);
            }

            // Luôn sắp xếp theo mới nhất
            query = query.OrderByDescending(log => log.Timestamp);

            // 3. Phân trang
            var totalItems = await query.CountAsync();
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginationResponse<AuditLog>
            {
                Items = items,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }
    }
}

