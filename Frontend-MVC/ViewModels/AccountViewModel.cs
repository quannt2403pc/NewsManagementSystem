using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Frontend_MVC.ViewModels
{
    public class AccountViewModel
    {
        [JsonPropertyName("accountId")]
        public int AccountId { get; set; }

        [JsonPropertyName("accountEmail")]
        public string AccountEmail { get; set; }

        [JsonPropertyName("accountName")]
        public string AccountName { get; set; }

        [JsonPropertyName("accountRole")]
        public int AccountRole { get; set; } // 1=Admin, 2=Staff?

        // Helper property for display
        public string RoleName => AccountRole == 1 ? "Staff" : (AccountRole == 2 ? "Lecturer" : "Unknown");
    }
    public class AccountListViewModel
    {
        public List<AccountViewModel> Accounts { get; set; } = new List<AccountViewModel>();
        public string? SearchString { get; set; }
        public int? SelectedRole { get; set; } // For filtering

        // For the role filter dropdown
        public SelectList RoleOptions { get; } = new SelectList(
                  new List<SelectListItem>
                  {
                new SelectListItem { Value = "1", Text = "Staff" },   
                new SelectListItem { Value = "2", Text = "Lecturer" }
                                                                
                  }, "Value", "Text");
    }


    public class AccountCreateViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string AccountEmail { get; set; }

        [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
        [Display(Name = "Tên hiển thị")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự.")]
        [Display(Name = "Mật khẩu")]
        public string AccountPassword { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu là bắt buộc.")]
        [DataType(DataType.Password)]
        [Compare("AccountPassword", ErrorMessage = "Mật khẩu và xác nhận mật khẩu không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc.")]
        [Display(Name = "Vai trò")]
        public int AccountRole { get; set; }
    }
    public class AccountUpdateViewModel
    {
        [Required]
        public int AccountId { get; set; } // Hidden field

        [Required(ErrorMessage = "Email là bắt buộc.")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [Display(Name = "Email")]
        public string AccountEmail { get; set; }

        [Required(ErrorMessage = "Tên hiển thị là bắt buộc.")]
        [Display(Name = "Tên hiển thị")]
        public string AccountName { get; set; }

        [Required(ErrorMessage = "Vai trò là bắt buộc.")]
        [Display(Name = "Vai trò")]
        public int AccountRole { get; set; }
    }
    public class ChangePasswordViewModel1
    {
        [Required]
        public int AccountId { get; set; } // Hidden field

        [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string CurrentPassword { get; set; }

        [Required(ErrorMessage = "Mật khẩu mới là bắt buộc.")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải có ít nhất 6 ký tự.")]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Xác nhận mật khẩu mới là bắt buộc.")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu mới và xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmNewPassword { get; set; }
    }



}
