namespace BusinessOperations.Application.DTOs.Auth;

public class CurrentUserDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public string? FullName { get; set; }
    public string? DepartmentName { get; set; }
}
