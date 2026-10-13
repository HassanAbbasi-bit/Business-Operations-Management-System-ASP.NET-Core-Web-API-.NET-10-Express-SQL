namespace BusinessOperations.Application.DTOs.Auth;

public class AuthResponseDto
{
    public string Token { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? EmployeeId { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
}
