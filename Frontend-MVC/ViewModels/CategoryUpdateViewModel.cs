using System.ComponentModel.DataAnnotations;

namespace Frontend_MVC.ViewModels
{
   
        public class CategoryCreateUpdateViewModel
        {
            // CategoryId sẽ = 0 khi tạo mới, > 0 khi cập nhật
            public int CategoryId { get; set; }

            [Required(ErrorMessage = "Tên danh mục là bắt buộc.")]
            [Display(Name = "Tên Danh mục")]
            public string CategoryName { get; set; }

            [Required(ErrorMessage = "Mô tả là bắt buộc.")]
            [Display(Name = "Mô tả")]
            public string CategoryDescription { get; set; }

            [Display(Name = "Danh mục cha")]
            public int? ParentCategoryId { get; set; } // Cho phép null

            [Display(Name = "Trạng thái")]
            public bool IsActive { get; set; } = true; // Mặc định là Hoạt động

            // --- Thuộc tính bổ sung cho Dropdown (không gửi lên API) ---
            // Danh sách này sẽ được Controller đổ vào
            public List<CategoryViewModel>? AvailableParentCategories { get; set; }
        }
}
