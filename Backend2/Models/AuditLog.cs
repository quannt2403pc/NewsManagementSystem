using System;
using System.Collections.Generic;

namespace Backend2.Models;

public partial class AuditLog
{
    public int AuditLogId { get; set; }

    public string? UserEmail { get; set; }

    public string? Action { get; set; }

    public string? EntityName { get; set; }

    public DateTime Timestamp { get; set; }

    public string? KeyValues { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }
}
