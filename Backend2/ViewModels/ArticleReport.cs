namespace Backend2.ViewModels
{
    public class ArticleReport
    {
        public string GroupName { get; set; } // Tên nhóm (Category, Author, Status)
        public int ArticleCount { get; set; }
    }

    // BusinessObject/ArticleTotalReport.cs
    public class ArticleTotalReport
    {
        public int TotalActive { get; set; }
        public int TotalInactive { get; set; }
    }
}
