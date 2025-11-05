using AnalyticApi.Dtos;

namespace AnalyticApi.Repositories
{
    public interface IAnalyticRepository
    {
        Task<DashboardDataDto> GetDashboardDataAsync(DateTime? startDate, DateTime? endDate, int? categoryId, bool? status);

        // Lấy dữ liệu cho /trending
        Task<List<TrendingArticleDto>> GetTrendingArticlesAsync(int topN = 5);

        // Lấy dữ liệu cho /recommend/{id}

        // Lấy dữ liệu (dạng byte[]) cho /export
        Task<byte[]> ExportDashboardDataAsync(DateTime? startDate, DateTime? endDate, int? categoryId, bool? status);
    }
}
