using Backend2.ViewModels;

namespace Backend2.Repositories.Interface
{
    public interface INewsArticleRepositoryV2
    {
        Task<PaginationResponse<NewsArticleDto>> GetNewsArticlesAsync(
               string? searchString,
               string? sortBy,
               string? sortDirection,
               int pageNumber,
               int pageSize
           );


    }
}
