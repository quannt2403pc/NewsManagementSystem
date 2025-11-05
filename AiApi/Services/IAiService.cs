namespace AiApi.Services
{
    /// <summary>
    /// Interface cho dịch vụ gợi ý tag.
    /// </summary>
    public interface IAiService
    {
        /// <summary>
        /// Gợi ý các tags dựa trên nội dung.
        /// </summary>
        /// <param name="content">Nội dung bài viết.</param>
        /// <returns>Danh sách các tag (chuỗi).</returns>
        Task<List<string>> SuggestTagsAsync(string content);
    }
}
