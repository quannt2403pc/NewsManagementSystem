using Backend2.Models;
using Backend2.Repositories.Interface;
using Backend2.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Backend2.Repositories.Class
{
    public class NewsArticleRepositoryV2 : INewsArticleRepositoryV2
    {
        private readonly Prn232Assignment1Context _context;

        public NewsArticleRepositoryV2(Prn232Assignment1Context context)
        {
            _context = context;
        }

        public async Task<PaginationResponse<NewsArticleDto>> GetNewsArticlesAsync(
           string? searchString,
           string? sortBy,
           string? sortDirection,
           int pageNumber,
           int pageSize)
        {
            var query = _context.NewsArticles
                                .Include(n => n.Category)
                                .AsQueryable();

            // 1. TÌM KIẾM
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(n => n.NewsTitle.Contains(searchString));
            }

            // 2. SẮP XẾP (ĐÃ SỬA LỖI)
            bool isDescending = sortDirection?.ToLower() == "desc";

            // Chuẩn hóa sortBy
            string sortColumn = string.IsNullOrWhiteSpace(sortBy) ? "createddate" : sortBy.ToLower();

            // Sửa lỗi: Xử lý từng cột một cách tường minh
            // KHÔNG dùng Expression<Func<T, object>>
            if (sortColumn == "newstitle")
            {
                query = isDescending
                    ? query.OrderByDescending(n => n.NewsTitle)
                    : query.OrderBy(n => n.NewsTitle);
            }
            else // Mặc định (hoặc "createddate")
            {
                // Nếu không chỉ định, mặc định là sắp xếp theo ngày tạo GIẢM DẦN
                if (sortBy == null) isDescending = true;

                query = isDescending
                    ? query.OrderByDescending(n => n.CreatedDate)
                    : query.OrderBy(n => n.CreatedDate);
            }

            // 3. PHÂN TRANG
            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var articles = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NewsArticleDto
                {
                    NewsArticleID = n.NewsArticleId,
                    NewsTitle = n.NewsTitle,
                    CreatedDate = n.CreatedDate,
                    NewsStatus = n.NewsStatus,
                    CategoryName = n.Category != null ? n.Category.CategoryName : "N/A"
                })
                .ToListAsync();

            // 4. Trả về kết quả
            return new PaginationResponse<NewsArticleDto>
            {
                Items = articles,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages
            };
        }



    }
}
