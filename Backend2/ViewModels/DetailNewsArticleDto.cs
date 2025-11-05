namespace Backend2.ViewModels
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
}
