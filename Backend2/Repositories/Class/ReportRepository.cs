using Backend2.Models;
using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Backend2.Repositories.Class
{
    public class ReportRepository :IReportRepository
    {
        private readonly Prn232Assignment1Context _context;

        public ReportRepository(Prn232Assignment1Context context)
        {
            _context = context;
        }

        public IEnumerable<ArticleReport> GetArticlesReport(
            DateTime? startDate,
            DateTime? endDate,
            string groupBy)
        {
            var query = _context.NewsArticles.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(na => na.CreatedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(na => na.CreatedDate <= endDate.Value);
            }

            switch (groupBy.ToLower())
            {
                case "category":
                    return query.Include(na => na.Category)
                                .GroupBy(na => na.Category.CategoryName)
                                .Select(g => new ArticleReport
                                {
                                    GroupName = g.Key,
                                    ArticleCount = g.Count()
                                })
                                .OrderByDescending(r => r.ArticleCount)
                                .ToList();
                case "author":
                    return query.Include(na => na.CreatedBy)
                                .GroupBy(na => na.CreatedBy.AccountName)
                                .Select(g => new ArticleReport
                                {
                                    GroupName = g.Key,
                                    ArticleCount = g.Count()
                                })
                                .OrderByDescending(r => r.ArticleCount)
                                .ToList();
                case "status":
                    return query.GroupBy(na => na.NewsStatus)
                                .Select(g => new ArticleReport
                                {
                                    GroupName = g.Key == true ? "Active" : "Inactive",
                                    ArticleCount = g.Count()
                                })
                                .OrderByDescending(r => r.ArticleCount)
                                .ToList();
                default:
                    return new List<ArticleReport>();
            }
        }

        public ArticleTotalReport GetArticleTotals(
            DateTime? startDate,
            DateTime? endDate)
        {
            var query = _context.NewsArticles.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(na => na.CreatedDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                query = query.Where(na => na.CreatedDate <= endDate.Value);
            }

            return new ArticleTotalReport
            {
                TotalActive = query.Count(na => na.NewsStatus == true),
                TotalInactive = query.Count(na => na.NewsStatus == false)
            };
        }
    }
}


