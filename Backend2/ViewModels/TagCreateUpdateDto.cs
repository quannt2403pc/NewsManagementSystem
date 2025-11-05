using System.ComponentModel.DataAnnotations;

namespace Backend2.ViewModels
{
    public class TagCreateUpdateDto
    {
        [Required(ErrorMessage = "Tag name is required.")]
        [StringLength(100, ErrorMessage = "Tag name cannot exceed 100 characters.")]
        public string TagName { get; set; }

        [StringLength(500, ErrorMessage = "Note cannot exceed 500 characters.")]
        public string? Note { get; set; }
    }
}
