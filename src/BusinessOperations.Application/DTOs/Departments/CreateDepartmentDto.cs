namespace BusinessOperations.Application.DTOs.Departments;

public class CreateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
}
