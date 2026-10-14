using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.DTOs.Auth;

public class RegisterRequestDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole? Role { get; set; }
    public Guid? DepartmentId { get; set; }
}
