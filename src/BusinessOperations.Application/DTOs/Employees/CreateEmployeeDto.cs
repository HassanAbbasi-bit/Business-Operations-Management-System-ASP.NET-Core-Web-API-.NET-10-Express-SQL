namespace BusinessOperations.Application.DTOs.Employees;

public class CreateEmployeeDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public DateOnly HireDate { get; set; }
    public int AnnualLeaveBalanceDays { get; set; } = 20;
    public Guid? UserId { get; set; }
}
