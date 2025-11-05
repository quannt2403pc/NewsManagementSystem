using System.ComponentModel.DataAnnotations;

namespace Backend2.ViewModels
{
    public class CategoryUpdateDto
    {
        [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
        public string CategoryName { get; set; }

        [Required(ErrorMessage = "Mô tả là bắt buộc.")]
        public string CategoryDescription { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool? IsActive { get; set; }
    }
}
