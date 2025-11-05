using Backend2.Models;
using Backend2.ViewModels;

namespace Backend2.Repositories.Interface
{
    public interface ICategoryRepository
    {
        IEnumerable<Category> GetCategories(string search = null);
        Category GetCategoryById(int categoryId);
        void AddCategory(Category category);
        void UpdateCategory(Category category);
        void DeleteCategory(int categoryId);
        bool HasNewsArticles(int categoryId);
        bool IsCategoryNameExist(string categoryName, int? categoryId = null);
        void ToggleActive(int categoryId);
        IEnumerable<CategoryWithArticleCount> GetCategoriesWithArticleCount(string? search = "");
    }
}
