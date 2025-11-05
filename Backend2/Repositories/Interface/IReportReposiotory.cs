using Backend2.ViewModels;

namespace Backend2.Repositories.Interface
{
    public interface IReportRepository
    {
        IEnumerable<ArticleReport> GetArticlesReport(
            DateTime? startDate,
            DateTime? endDate,
            string groupBy);

        ArticleTotalReport GetArticleTotals(
            DateTime? startDate,
            DateTime? endDate);
    }
}
