using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json.Serialization;

namespace Frontend_MVC.ViewModels
{
    public class DashboardViewModel
    {
        public DashboardDataDto DashboardData { get; set; }
        public List<TrendingArticleDto> TrendingArticles { get; set; }

        // Dữ liệu cho bộ lọc
        public List<CategoryViewModel> AvailableCategories { get; set; } = new List<CategoryViewModel>();
        public List<SelectListItem> StatusOptions { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "true", Text = "Hoạt động (Published)" },
            new SelectListItem { Value = "false", Text = "Ẩn (Draft)" }
        };

        // Các giá trị bộ lọc hiện tại
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? CategoryId { get; set; }
        public bool? Status { get; set; }
    }
    public class DashboardDataDto
    {
        [JsonPropertyName("categoryArticleCounts")]
        public List<ChartDataItem> CategoryArticleCounts { get; set; } = new List<ChartDataItem>();

        [JsonPropertyName("statusArticleCounts")]
        public List<ChartDataItem> StatusArticleCounts { get; set; } = new List<ChartDataItem>();
    }

    public class ChartDataItem
    {
        [JsonPropertyName("label")]
        public string Label { get; set; }
        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
    public class TrendingArticleDto
    {
        [JsonPropertyName("newsArticleID")]
        public int NewsArticleID { get; set; }
        [JsonPropertyName("newsTitle")]
        public string NewsTitle { get; set; }
        [JsonPropertyName("categoryName")]
        public string? CategoryName { get; set; }
        [JsonPropertyName("viewCount")]
        public int ViewCount { get; set; }
    }
}
