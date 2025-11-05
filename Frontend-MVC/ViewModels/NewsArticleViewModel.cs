using System.Text.Json.Serialization;

namespace Frontend_MVC.ViewModels
{
    // ViewModel cho một bài viết (Giống hệt bước trước)
    public class NewsArticleViewModel
    {
        public int NewsArticleID { get; set; }
        public string NewsTitle { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Headline { get; set; }
        public bool? NewsStatus { get; set; }
        public string? NewsContent { get; set; }
        public string? NewsSource { get; set; }
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public IFormFile? ImageFile { get; set; }
        public List<int> TagIds { get; set; } = new List<int>();
    }

    // ViewModel cho đối tượng phân trang (Giống hệt bước trước)
    public class PaginationResponseViewModel<T>
    {
        [JsonPropertyName("items")]
        public List<T> Items { get; set; }
        [JsonPropertyName("currentPage")]
        public int CurrentPage { get; set; }
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
    }

    // NÂNG CẤP: ViewModel chính cho View, chứa cả dữ liệu và trạng thái
    public class NewsListViewModel
    {
        // Dữ liệu trả về từ API
        public PaginationResponseViewModel<NewsArticleViewModel>? NewsResponse { get; set; }

        // Trạng thái hiện tại
        public string? SearchString { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
        public int CurrentPage { get; set; }
    }
}