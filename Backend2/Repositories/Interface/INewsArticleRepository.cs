using Backend2.Models;

namespace Backend2.Repositories.Interface
{
    public interface INewsArticleRepository
    {
        IEnumerable<NewsArticle> GetActiveNewsArticles();
        IEnumerable<NewsArticle> GetNewsArticles(
    string search = null,
    string authorName = null,
    string categoryName = null,
    bool? status = null,
    DateTime? startDate = null,
    DateTime? endDate = null,
    string sort = "desc");

        NewsArticle GetNewsArticleById(int newsArticleId);
        void AddNewsArticle(NewsArticle newsArticle, List<int>? tagIds);
        void UpdateNewsArticle(NewsArticle article, List<int> tagIds);
        public void DeleteNewsArticle(int newsArticleId);

        // Hàm này sẽ dùng cho bước 4 trong Controller
        void DuplicateNewsArticle(int newsArticleId, int createdById);
        NewsArticle GetNewsArticleDetails(int newsArticleId);
        List<NewsArticle> GetRelatedArticles(int newsArticleId);
    }
}
