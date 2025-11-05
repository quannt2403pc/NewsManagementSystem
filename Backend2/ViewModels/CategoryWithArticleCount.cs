namespace Backend2.ViewModels
{
    public class CategoryWithArticleCount
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public string CategoryDescription { get; set; }
        public bool IsActive { get; set; }
        public int ArticleCount { get; set; }

        public int? ParentCategoryId { get; set; }
        public string? ParentCategoryName { get; set; }

    }
}
