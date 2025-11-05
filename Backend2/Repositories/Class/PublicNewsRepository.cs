using Backend2.Models;
using Backend2.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Backend2.Repositories.Class
{
    public class PublicNewsRepository : IPublicNewsRepository
    {
        private readonly Prn232Assignment1Context _context;

        public PublicNewsRepository(Prn232Assignment1Context context)
        {
            _context = context;
        }

        public IEnumerable<NewsArticle> SearchPublicNews(
            string search,
            string categoryName,
            string tagName,
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _context.NewsArticles
                                .Include(na => na.Category)
                                .Include(na => na.Tags)
                                .Include(na => na.CreatedBy)
                                .Where(na => na.NewsStatus == true)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(na =>
                    na.NewsTitle.Contains(search) ||
                    na.NewsContent.Contains(search) ||
                    na.Headline.Contains(search)
                );
            }

            if (!string.IsNullOrEmpty(categoryName))
            {
                query = query.Where(na => na.Category.CategoryName.Contains(categoryName));
            }

            if (!string.IsNullOrEmpty(tagName))
            {
                query = query.Where(na => na.Tags.Any(t => t.TagName.Contains(tagName)));
            }

            if (startDate.HasValue)
            {
                query = query.Where(na => na.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(na => na.CreatedDate <= endDate.Value);
            }

            return query.OrderByDescending(na => na.CreatedDate).ToList();
        }
    }
}
