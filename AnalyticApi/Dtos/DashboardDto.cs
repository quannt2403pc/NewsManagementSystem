namespace AnalyticApi.Dtos
{
    public class DashboardDataDto
    {
        public List<ChartDataItem> CategoryArticleCounts { get; set; }
        public List<ChartDataItem> StatusArticleCounts { get; set; }
    }
    public class ChartDataItem
    {
        public string Label { get; set; } // Ví dụ: "Technology" hoặc "Published"
        public int Value { get; set; }   // Ví dụ: 10
    }

    // AnalyticsAPI/ViewModels/TrendingArticleDto.cs
    public class TrendingArticleDto
    {
        public int NewsArticleID { get; set; }
        public string NewsTitle { get; set; }
        public string CategoryName { get; set; }
        public int ViewCount { get; set; } // Giả định có trường này
    }
}
