using Backend2.Models;
using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Backend2.Repositories.Class
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly    Prn232Assignment1Context _context;

        public CategoryRepository(Prn232Assignment1Context context)
        {
            _context = context;
        }

        public void AddCategory(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
        }

        public void DeleteCategory(int categoryId)
        {
            var category = _context.Categories.Find(categoryId);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }

        }

        public IEnumerable<Category> GetCategories(string search = null)
        {
            var query = _context.Categories.Include(c=>c.NewsArticles).AsQueryable();
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c =>
                    c.CategoryName.Contains(search) ||
                    c.CategoryDescription.Contains(search)
                );
            }
            return query.ToList();
        }

        public IEnumerable<CategoryWithArticleCount> GetCategoriesWithArticleCount(string? search = "")
        {
            var query = _context.Categories
                .Include(c => c.ParentCategory)
                .Select(c => new CategoryWithArticleCount
                {
                    CategoryID = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryDescription = c.CategoryDescription,
                    IsActive = c.IsActive.Value,
                    ArticleCount = c.NewsArticles.Count(),
                    ParentCategoryId = c.ParentCategoryId,
                    ParentCategoryName = c.ParentCategory != null ? c.ParentCategory.CategoryName : null
                })
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                query = query.Where(c =>
                    c.CategoryName.ToLower().Contains(search) ||
                    (c.CategoryDescription != null && c.CategoryDescription.ToLower().Contains(search))
                );
            }

            return query.ToList();
        }

        public Category GetCategoryById(int categoryId)
        {
            return _context.Categories.Find(categoryId);
        }

        public bool HasNewsArticles(int categoryId)
        {
            return _context.NewsArticles.Any(na => na.CategoryId == categoryId);
        }

        public bool IsCategoryNameExist(string categoryName, int? categoryId = null)
        {
            var query = _context.Categories.Where(c => c.CategoryName == categoryName);
            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId != categoryId.Value);
            }
            return query.Any();
        }

        public void ToggleActive(int categoryId)
        {
            var category = _context.Categories.Find(categoryId);
            if (category != null)
            {
                category.IsActive = !category.IsActive;
                _context.SaveChanges();
            }
        }

        public void UpdateCategory(Category category)
        {
            var existing = _context.Categories
                .FirstOrDefault(c => c.CategoryId == category.CategoryId);

            if (existing == null)
                throw new Exception("Category not found");

            existing.CategoryName = category.CategoryName;
            existing.CategoryDescription = category.CategoryDescription;
            existing.ParentCategoryId = category.ParentCategoryId;

            _context.SaveChanges();
        }
    }
}
