using Backend2.Models;
using Backend2.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Backend2.Repositories.Class
{
    public class NewsArticleRepository : INewsArticleRepository
    {
        Prn232Assignment1Context _context;

        public NewsArticleRepository(Prn232Assignment1Context context)
        {
            _context = context;
        }
        public void AddNewsArticle(NewsArticle newsArticle, List<int>? tagIds)
        {
            newsArticle.CreatedDate = DateTime.Now;

            // Gắn các Tag có sẵn
            if (tagIds != null && tagIds.Count > 0)
            {
                var tags = _context.Tags
                    .Where(t => tagIds.Contains(t.TagId))
                    .ToList();

                newsArticle.Tags = tags;
            }

            _context.NewsArticles.Add(newsArticle);
            _context.SaveChanges(); // EF tự thêm dòng vào bảng NewsTag
        }

        public void DeleteNewsArticle(int newsArticleId)
        {
            var newsArticle = _context.NewsArticles.Find(newsArticleId);
            if (newsArticle != null)
            {
                newsArticle.Tags.Clear();
                _context.NewsArticles.Remove(newsArticle);
                _context.SaveChanges();
            }

        }

        public void DuplicateNewsArticle(int newsArticleId, int createdById)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<NewsArticle> GetActiveNewsArticles()
        {
            return _context.NewsArticles
                                      .Include(na => na.Category)
                                      .Include(na => na.CreatedBy)
                                       .Include(na => na.Tags)
                                      .Where(na => na.NewsStatus == true)

                                      .ToList();
        }





        public NewsArticle GetNewsArticleById(int newsArticleId)
        {
            return _context.NewsArticles
                           .Include(na => na.Tags)

                           .FirstOrDefault(na => na.NewsArticleId == newsArticleId);
        }

        public NewsArticle GetNewsArticleDetails(int newsArticleId)
        {
            return _context.NewsArticles
                           .Include(na => na.Category)
                           .Include(na => na.Tags)
                           .Include(na => na.CreatedBy) // Tải thông tin người tạo
                           .Include(na => na.UpdatedBy) // Tải thông tin người cập nhật
                           .FirstOrDefault(na => na.NewsArticleId == newsArticleId);
        }
        public IEnumerable<NewsArticle> GetNewsArticles(
         string? search = null,
         string? authorName = null,
         string? categoryName = null,
         bool? status = null,
         DateTime? startDate = null,
         DateTime? endDate = null,
         string? sort = "desc")
        {
            var query = _context.NewsArticles
                                .Include(na => na.Category)
                                .Include(na => na.CreatedBy)
                                 .Include(na => na.Tags)
                                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(na =>
                    (na.NewsTitle != null && na.NewsTitle.Contains(search)) ||
                    (na.NewsContent != null && na.NewsContent.Contains(search)) ||
                    (na.Headline != null && na.Headline.Contains(search))
                );
            }

            if (!string.IsNullOrWhiteSpace(authorName))
            {
                query = query.Where(na => na.CreatedBy != null &&
                                          na.CreatedBy.AccountName != null &&
                                          na.CreatedBy.AccountName.Contains(authorName));
            }

            if (!string.IsNullOrWhiteSpace(categoryName))
            {
                query = query.Where(na => na.Category != null &&
                                          na.Category.CategoryName != null &&
                                          na.Category.CategoryName.Contains(categoryName));
            }

            if (status.HasValue)
            {
                query = query.Where(na => na.NewsStatus == status.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(na => na.CreatedDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(na => na.CreatedDate <= endDate.Value);
            }

            // Sắp xếp an toàn
            if (string.IsNullOrWhiteSpace(sort) || sort.ToLower() == "desc")
            {
                query = query.OrderByDescending(na => na.CreatedDate);
            }
            else
            {
                query = query.OrderBy(na => na.CreatedDate);
            }

            return query.ToList();
        }

        public List<NewsArticle> GetRelatedArticles(int newsArticleId)
        {
            var currentArticle = _context.NewsArticles
                                                 .Include(na => na.Tags)
                                                 .FirstOrDefault(na => na.NewsArticleId == newsArticleId);

            if (currentArticle == null)
            {
                return new List<NewsArticle>(); 
            }

            var currentCategoryId = currentArticle.CategoryId;
            var currentTagIds = currentArticle.Tags.Select(t => t.TagId).ToList();

            var relatedArticles = _context.NewsArticles
                .Where(na =>
                    na.NewsArticleId != newsArticleId && 
                    na.NewsStatus == true && 
                    (
                        na.CategoryId == currentCategoryId || 
                        na.Tags.Any(tag => currentTagIds.Contains(tag.TagId)) 
                    )
                )
                .OrderByDescending(na => na.CreatedDate) 
                .Take(3) 
                .ToList();

            return relatedArticles;
        }

        public void UpdateNewsArticle(NewsArticle article, List<int> tagIds)
        {
            var existingArticle = _context.NewsArticles
                .Include(na => na.Tags)
                .FirstOrDefault(na => na.NewsArticleId == article.NewsArticleId);

            if (existingArticle == null)
            {
                throw new Exception("Article not found");
            }

            _context.Entry(existingArticle).CurrentValues.SetValues(article);

            existingArticle.Tags.Clear(); 

            if (tagIds != null && tagIds.Any())
            {
                var tags = _context.Tags
                    .Where(t => tagIds.Contains(t.TagId))
                    .ToList();

                foreach (var tag in tags)
                {
                    existingArticle.Tags.Add(tag);
                }
            }

            _context.SaveChanges();
        }





    }


}
