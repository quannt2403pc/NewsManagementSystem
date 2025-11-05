namespace Frontend_MVC.ViewModels
{
    public class CategoryListViewModel
    {
        public List<CategoryWithArticleCount> Categories { get; set; } = new List<CategoryWithArticleCount>();
        public string? SearchString { get; set; }
    }
}
