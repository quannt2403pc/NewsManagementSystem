using AnalyticApi.Dtos;
using AnalyticApi.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace AnalyticApi.Repositories
{
    public class AnalyticRepository : IAnalyticRepository
    {
        private readonly Prn232Assignment1Context _context;
        private IQueryable<NewsArticle> GetFilteredArticles(DateTime? startDate, DateTime? endDate, int? categoryId, bool? status)
        {
            var query = _context.NewsArticles.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedDate <= endDate.Value);

            if (categoryId.HasValue)
                query = query.Where(a => a.CategoryId == categoryId.Value);

            if (status.HasValue)
                query = query.Where(a => a.NewsStatus == status.Value);

            return query;
        }
        public AnalyticRepository(Prn232Assignment1Context context)
        {
            _context = context;
        }
        public async Task<byte[]> ExportDashboardDataAsync(DateTime? startDate, DateTime? endDate, int? categoryId, bool? status)
        {
            // Lấy dữ liệu đã lọc (giống hệt hàm GetDashboardDataAsync)
            var data = await GetDashboardDataAsync(startDate, endDate, categoryId, status);

            using (var workbook = new XLWorkbook())
            {
                // Sheet 1: Thống kê theo Danh mục
                var categorySheet = workbook.Worksheets.Add("Theo Danh Muc");
                categorySheet.Cell("A1").Value = "Tên Danh Mục";
                categorySheet.Cell("B1").Value = "Số Lượng Bài Viết";
                categorySheet.Row(1).Style.Font.Bold = true;

                int row = 2;
                foreach (var item in data.CategoryArticleCounts)
                {
                    categorySheet.Cell(row, 1).Value = item.Label;
                    categorySheet.Cell(row, 2).Value = item.Value;
                    row++;
                }

                // Sheet 2: Thống kê theo Trạng thái
                var statusSheet = workbook.Worksheets.Add("Theo Trang Thai");
                statusSheet.Cell("A1").Value = "Trạng Thái";
                statusSheet.Cell("B1").Value = "Số Lượng Bài Viết";
                statusSheet.Row(1).Style.Font.Bold = true;

                row = 2;
                foreach (var item in data.StatusArticleCounts)
                {
                    statusSheet.Cell(row, 1).Value = item.Label;
                    statusSheet.Cell(row, 2).Value = item.Value;
                    row++;
                }

                // Tự động căn chỉnh cột
                categorySheet.Columns().AdjustToContents();
                statusSheet.Columns().AdjustToContents();

                // Lưu vào MemoryStream và trả về mảng byte[]
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        public async Task<DashboardDataDto> GetDashboardDataAsync(DateTime? startDate, DateTime? endDate, int? categoryId, bool? status)
        {
            var filteredQuery = GetFilteredArticles(startDate, endDate, categoryId, status);

            // 1. Thống kê theo Category
            var categoryCounts = await filteredQuery
                .Include(a => a.Category) // Join với bảng Category
                .Where(a => a.Category != null)
                .GroupBy(a => a.Category.CategoryName)
                .Select(g => new ChartDataItem
                {
                    Label = g.Key,
                    Value = g.Count()
                })
                .OrderByDescending(d => d.Value)
                .ToListAsync();

            // 2. Thống kê theo Status
            var statusCounts = await filteredQuery
                .GroupBy(a => a.NewsStatus ?? false) // Nhóm theo true/false
                .Select(g => new ChartDataItem
                {
                    Label = g.Key ? "Published" : "Draft",
                    Value = g.Count()
                })
                .ToListAsync();

            return new DashboardDataDto
            {
                CategoryArticleCounts = categoryCounts,
                StatusArticleCounts = statusCounts
            };
        }


        public async Task<List<TrendingArticleDto>> GetTrendingArticlesAsync(int topN = 5)
        {
            // Giả định NewsArticle có trường 'ViewCount'
            return await _context.NewsArticles
                .Where(a => a.NewsStatus == true)
                .OrderByDescending(a => a.NewsArticleId) // <-- Cần có trường ViewCount
                .Take(topN)
                .Include(a => a.Category)
                .Select(a => new TrendingArticleDto
                {
                    NewsArticleID = a.NewsArticleId,
                    NewsTitle = a.NewsTitle,
                    CategoryName = a.Category.CategoryName,
                     ViewCount= a.NewsArticleId // Giả định ViewCount có thể null
                })
                .ToListAsync();
        }
    }
}

