using Backend2.Models;
using Backend2.Repositories.Interface;
using Microsoft.EntityFrameworkCore;

namespace Backend2.Repositories.Class
{
    public class TagRepository : ITagRepository
    {
        private readonly Prn232Assignment1Context _context;

        public TagRepository(Prn232Assignment1Context context)
        {
            _context = context;
        }

        public IEnumerable<Tag> GetTags(string search = null)
        {
           var query = _context.Tags.Include(t => t.NewsArticles).AsEnumerable();
            ;
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(t => t.TagName.Contains(search)).ToList();
            }
            return query;
        }

        public Tag GetTagById(int tagId)
        {
            return _context.Tags.FirstOrDefault(t => t.TagId == tagId);
        }

        public void AddTag(Tag tag)
        {
            _context.Tags.Add(tag);
            _context.SaveChanges();
        }

        public void UpdateTag(Tag tag)
        {
            _context.Tags.Update(tag);
            _context.SaveChanges();
        }

        public void DeleteTag(int tagId)
        {
            var tag = _context.Tags.FirstOrDefault(t => t.TagId == tagId);
            if (tag != null)
            {
                _context.Tags.Remove(tag);
                _context.SaveChanges();
            }
        }

        public bool IsTagNameExist(string tagName, int? tagId = null)
        {
            var query = _context.Tags.Where(t => t.TagName == tagName);
            if (tagId.HasValue)
            {
                query = query.Where(t => t.TagId != tagId.Value);
            }
            return query.Any();
        }

        public bool IsTagReferencedInNews(int tagId)
        {
            return _context.NewsArticles.Any(na => na.Tags.Any(t => t.TagId == tagId));
        }
    }
}
