using Backend2.Models;

namespace Backend2.Repositories.Interface
{
    public interface ITagRepository
    {
        IEnumerable<Tag> GetTags(string search = null);
        Tag GetTagById(int tagId);
        void AddTag(Tag tag);
        void UpdateTag(Tag tag);
        void DeleteTag(int tagId);
        bool IsTagNameExist(string tagName, int? tagId = null);
        bool IsTagReferencedInNews(int tagId);
    }
}
