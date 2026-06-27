using System.ComponentModel.DataAnnotations;

namespace EmployeeMvcAuth.Models
{
    public class PasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
