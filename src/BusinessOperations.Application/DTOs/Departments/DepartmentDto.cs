namespace BusinessOperations.Application.DTOs.Departments;

public class DepartmentDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
    public int EmployeeCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
