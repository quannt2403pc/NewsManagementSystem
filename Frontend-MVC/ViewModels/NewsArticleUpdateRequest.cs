// BusinessObject/NewsArticleUpdateRequest.cs
using Frontend_MVC.ViewModels;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public class NewsArticleUpdateRequest
{
    public int NewsArticleId { get; set; } // Sẽ không dùng khi Create, nhưng để tương thích

    [Required(ErrorMessage = "Tiêu đề là bắt buộc")] // Thêm validation nếu cần
    [Display(Name = "Tiêu đề")]
    public string NewsTitle { get; set; }

    [Display(Name = "Tiêu đề phụ (Headline)")]
    public string Headline { get; set; }

    [Display(Name = "Nội dung")]
    public string NewsContent { get; set; }

    [Display(Name = "Nguồn tin")]
    public string NewsSource { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn danh mục")] // Thêm validation nếu cần
    [Display(Name = "Danh mục")]
    public int? CategoryId { get; set; }

    [Display(Name = "Hoạt động")]
    public bool? NewsStatus { get; set; } = true; // Giá trị mặc định

    [Display(Name = "Các Thẻ (Tags)")]
    public List<int> TagIds { get; set; } = new List<int>(); // Khởi tạo List

    // Thuộc tính bổ sung cho View và Upload
    [Display(Name = "Ảnh bìa")]
    public IFormFile? ImageFile { get; set; } // Cần cho upload

    // Thuộc tính bổ sung cho Dropdowns (không gửi lên API)
    public List<CategoryViewModel> Categories { get; set; } = new List<CategoryViewModel>();
    public List<TagViewModel> Tags { get; set; } = new List<TagViewModel>();
}