namespace EmployeeMvcAuth.Models
{
    public class EmployeeSalaryViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        public decimal BasicSalary { get; set; }
        public decimal HouseRent { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal Bonus { get; set; }
        public decimal TotalSalary { get; set; }

        public DateTime EffectiveFrom { get; set; }
    }
}
