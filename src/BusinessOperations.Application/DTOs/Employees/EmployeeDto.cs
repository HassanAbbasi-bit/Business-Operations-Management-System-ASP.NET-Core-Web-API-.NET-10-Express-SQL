namespace BusinessOperations.Application.DTOs.Employees;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? ManagerId { get; set; }
    public string? ManagerName { get; set; }
    public DateOnly HireDate { get; set; }
    public int AnnualLeaveBalanceDays { get; set; }
    public bool IsActive { get; set; }
}
