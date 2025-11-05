using System.Text.Json.Serialization;

namespace Frontend_MVC.ViewModels
{
    public class DetailNewsArticleDto
    {
        public int NewsArticleID { get; set; }
        public string NewsTitle { get; set; }
        public string? Headline { get; set; }
        public string? NewsContent { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? ImageUrl { get; set; }
        public string? CategoryName { get; set; }
        public string? AuthorName { get; set; }
        public List<string> TagNames { get; set; } = new List<string>();

        public DateTime? ModifiedDate { get; set; }
        public string? UpdatedByName { get; set; }
    }
    public class NewsArticleDetailViewModel
    {
        // 1. Dữ liệu từ Core API (GET /api/NewsArticleV2/{id})
        public DetailNewsArticleDto Article { get; set; }

        // 2. Dữ liệu từ Analytics API (GET /api/recommend/{id})
        public List<ArticleSuggestionDto> RelatedArticles { get; set; } = new List<ArticleSuggestionDto>();
    }

    public class ArticleSuggestionDto
    {
        // Tên các thuộc tính này phải khớp với JSON trả về từ /api/recommend
        [JsonPropertyName("newsArticleID")]
        public int NewsArticleID { get; set; }

        [JsonPropertyName("newsTitle")]
        public string NewsTitle { get; set; }

        [JsonPropertyName("headline")]
        public string? Headline { get; set; }
    }




}
