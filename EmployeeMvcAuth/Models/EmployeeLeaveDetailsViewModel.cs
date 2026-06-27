namespace EmployeeMvcAuth.Models
{
    public class EmployeeLeaveDetailsViewModel
    {
        public bool IsSessionValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public List<EmployeeLeaveItemViewModel> LeaveItems { get; set; } = new();
    }

    public class EmployeeLeaveItemViewModel
    {
        public string LeaveType { get; set; } = string.Empty;
        public int TotalLeave { get; set; }
        public int UsedLeave { get; set; }
        public int RemainingLeave { get; set; }
        public int LeaveYear { get; set; }
    }
}
