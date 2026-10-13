namespace BusinessOperations.Application.DTOs.Employees;

public class UpdateEmployeeDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public int? AnnualLeaveBalanceDays { get; set; }
    public bool? IsActive { get; set; }
}
