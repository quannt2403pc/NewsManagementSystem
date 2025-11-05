using Backend2.Models;

namespace Backend2.Repositories.Interface
{
    public interface IPublicNewsRepository
    {
        IEnumerable<NewsArticle> SearchPublicNews(
         string search,
         string categoryName,
         string tagName,
         DateTime? startDate,
         DateTime? endDate);
    }
}
