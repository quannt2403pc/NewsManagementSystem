namespace Backend2.Services
{
    public interface IAuditLogService
    {
        Task LogAsync(string userEmail, string action, string entityName, string keyValues, object? oldValues, object? newValues);
    }
}
