using System.ComponentModel.DataAnnotations;

namespace AiApi.ViewModels
{
    public class SuggestTagRequest
    {
        [Required(ErrorMessage = "Nội dung không được để trống")]
        public string Content { get; set; }
    }
}
