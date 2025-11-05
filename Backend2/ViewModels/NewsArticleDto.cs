namespace Backend2.ViewModels
{
    public class NewsArticleDto
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
}
