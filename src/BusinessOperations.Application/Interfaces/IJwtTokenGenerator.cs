using BusinessOperations.Domain.Entities;

namespace BusinessOperations.Application.Interfaces;

/// <summary>
/// Service responsible for minting cryptographically signed JWT bearer tokens.
/// </summary>
public interface IJwtTokenGenerator
{
    /// <summary>
    /// Generates a signed JWT bearer token containing user and role claims.
    /// </summary>
    (string Token, DateTime ExpiresAtUtc) GenerateToken(User user, Employee? employee = null);
}
