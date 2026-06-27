using System.ComponentModel.DataAnnotations;

namespace EmployeeMvcAuth.Models
{
    public class LoginUserIdViewModel
    {
        [Required]
        [Display(Name = "User ID")]
        public string UserId { get; set; } = string.Empty;
    }
}
