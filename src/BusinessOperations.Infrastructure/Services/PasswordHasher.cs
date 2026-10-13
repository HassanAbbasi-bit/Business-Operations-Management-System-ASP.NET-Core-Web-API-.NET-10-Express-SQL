using System.Security.Cryptography;
using BusinessOperations.Application.Interfaces;

namespace BusinessOperations.Infrastructure.Services;

/// <summary>
/// Secure PBKDF2-HMAC-SHA256 password hasher with cryptographic salt
/// and constant-time equality verification.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16; // 128 bit
    private const int KeySize = 32;  // 256 bit
    private const int Iterations = 100_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;
    private const char Delimiter = ':';

    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            KeySize);

        return string.Join(
            Delimiter,
            Convert.ToBase64String(salt),
            Convert.ToBase64String(hash),
            Iterations);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordHash))
            return false;

        string[] parts = passwordHash.Split(Delimiter);
        if (parts.Length != 3)
            return false;

        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);
            int iterations = int.Parse(parts[2]);

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations,
                Algorithm,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch
        {
            return false;
        }
    }
}
