using System.Text.Json.Serialization;

namespace Frontend_MVC.ViewModels
{
    public class CategoryWithArticleCount
    {
        [JsonPropertyName("categoryID")]
        public int CategoryID { get; set; }

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; }

        [JsonPropertyName("categoryDescription")]
        public string CategoryDescription { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("articleCount")]
        public int ArticleCount { get; set; }

        [JsonPropertyName("parentCategoryId")]
        public int? ParentCategoryId { get; set; }

        [JsonPropertyName("parentCategoryName")]
        public string? ParentCategoryName { get; set; }

    }
}
