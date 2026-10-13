namespace BusinessOperations.Application.DTOs.Departments;

public class UpdateDepartmentDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ManagerEmployeeId { get; set; }
}
