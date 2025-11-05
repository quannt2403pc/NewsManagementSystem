using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Frontend_MVC.ViewModels
{
    public class TagViewModel
    {
        [JsonPropertyName("tagId")]
        public int TagId { get; set; }

        [JsonPropertyName("tagName")]
        public string TagName { get; set; }

        // --- ADD THIS (if GET /api/tag returns it) ---
        [JsonPropertyName("note")]
        public string? Note { get; set; }
    }

    public class TagCreateUpdateViewModel
    {
        public int TagId { get; set; }

        [Required(ErrorMessage = "Tên thẻ là bắt buộc.")]
        [StringLength(100, ErrorMessage = "Tên thẻ không được vượt quá 100 ký tự.")]
        [Display(Name = "Tên Thẻ")]
        public string TagName { get; set; }

        // --- ADD THIS ---
        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
        // -------------
    }
    public class TagListViewModel
    {
        public List<TagViewModel> Tags { get; set; } = new List<TagViewModel>();
        public string? SearchString { get; set; }
    }



}
