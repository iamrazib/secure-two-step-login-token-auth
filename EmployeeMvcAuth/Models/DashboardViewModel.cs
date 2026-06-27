namespace EmployeeMvcAuth.Models
{
    public class DashboardViewModel
    {
        public Guid EmployeeKey { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
    }
}
