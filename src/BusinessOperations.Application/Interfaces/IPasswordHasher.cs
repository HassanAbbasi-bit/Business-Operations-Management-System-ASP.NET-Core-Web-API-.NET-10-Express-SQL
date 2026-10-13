namespace BusinessOperations.Application.Interfaces;

/// <summary>
/// Cryptographic hashing service for secure user credential management.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Computes a cryptographically salted hash of the plaintext password.
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies whether the supplied plaintext password matches the stored hash.
    /// </summary>
    bool VerifyPassword(string password, string passwordHash);
}
