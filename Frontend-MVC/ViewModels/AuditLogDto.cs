using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json.Serialization;

namespace Frontend_MVC.ViewModels
{
    public class AuditLogDto
    {
        [JsonPropertyName("auditLogId")]
        public int AuditLogId { get; set; }

        [JsonPropertyName("userEmail")]
        public string? UserEmail { get; set; }

        [JsonPropertyName("action")]
        public string? Action { get; set; }

        [JsonPropertyName("entityName")]
        public string? EntityName { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }

        [JsonPropertyName("keyValues")]
        public string? KeyValues { get; set; }

        [JsonPropertyName("oldValues")]
        public string? OldValues { get; set; } // Đây là một chuỗi JSON

        [JsonPropertyName("newValues")]
        public string? NewValues { get; set; } // Đây là một chuỗi JSON
    }
    public class AuditLogListViewModel
    {
        // Dùng lại PaginationResponseViewModel<T> từ NewsArticle
        public PaginationResponseViewModel<AuditLogDto> LogResponse { get; set; }

        // Dữ liệu cho bộ lọc
        public string? UserEmail { get; set; }
        public string? EntityName { get; set; }
        public int CurrentPage { get; set; }

        // Dropdown cho bộ lọc EntityName
        public SelectList EntityOptions { get; } = new SelectList(
            new List<SelectListItem>
            {
                new SelectListItem { Value = "NewsArticle", Text = "Bài viết (NewsArticle)" },
                new SelectListItem { Value = "Category", Text = "Danh mục (Category)" },
                new SelectListItem { Value = "Tag", Text = "Thẻ (Tag)" },
                new SelectListItem { Value = "SystemAccount", Text = "Tài khoản (SystemAccount)" }
            }, "Value", "Text");
    }
}
