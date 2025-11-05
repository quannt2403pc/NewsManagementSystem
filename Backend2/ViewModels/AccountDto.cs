using System.ComponentModel.DataAnnotations;

namespace Backend2.ViewModels
{
    public class AccountDto
    {
        public int AccountId { get; set; }
        public string AccountEmail { get; set; }
        public string AccountName { get; set; } // Assuming Full Name field
        public int AccountRole { get; set; }
    }
    public class AccountCreateDto
    {
        [Required, EmailAddress]
        public string AccountEmail { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters.")]
        public string AccountPassword { get; set; } // Still receiving plain text here

        [Required]
        public string AccountName { get; set; } // Assuming Full Name

        [Required]
        [Range(1, 2, ErrorMessage = "Invalid Role ID.")] // Adjust range based on your roles
        public int AccountRole { get; set; }
    }
    public class AccountUpdateDto
    {
        [Required, EmailAddress]
        public string AccountEmail { get; set; }

        [Required]
        public string AccountName { get; set; }

        [Required]
        [Range(1, 2, ErrorMessage = "Invalid Role ID.")]
        public int AccountRole { get; set; }
    }
}
