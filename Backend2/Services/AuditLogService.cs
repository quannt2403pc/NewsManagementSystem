
using Backend2.Models;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Backend2.Services
{
    public class AuditLogService : IAuditLogService
    {
         private readonly Prn232Assignment1Context _context;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            WriteIndented = false 
        };
        public AuditLogService(Prn232Assignment1Context context)
        {
            _context = context;
        }
        public async Task LogAsync(string userEmail, string action, string entityName, string keyValues, object? oldValues, object? newValues)
        {
            var log = new AuditLog
            {
                UserEmail = userEmail,
                Action = action,
                EntityName = entityName,
                Timestamp = DateTime.Now, // Sử dụng DateTime.Now hoặc DateTime.UtcNow
                KeyValues = keyValues,

                // Serialize giá trị cũ và mới sang JSON
                OldValues = oldValues == null ? null : JsonSerializer.Serialize(oldValues, _jsonOptions),
                NewValues = newValues == null ? null : JsonSerializer.Serialize(newValues, _jsonOptions)
            };

            // Thêm log và lưu vào CSDL (trong một giao dịch riêng)
            _context.AuditLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}
