using BusinessOperations.Infrastructure.Services;

namespace BusinessOperations.UnitTests.Services;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ProducesDifferentHashes_ForSamePasswordDueToSalt()
    {
        // Arrange
        const string password = "SecurePassword123!";

        // Act
        string hash1 = _hasher.HashPassword(password);
        string hash2 = _hasher.HashPassword(password);

        // Assert
        Assert.NotEmpty(hash1);
        Assert.NotEmpty(hash2);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ReturnsTrue()
    {
        // Arrange
        const string password = "CorrectPassword123#";
        string hash = _hasher.HashPassword(password);

        // Act
        bool result = _hasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        const string password = "CorrectPassword123#";
        string hash = _hasher.HashPassword(password);

        // Act
        bool result = _hasher.VerifyPassword("WrongPassword456!", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_WithInvalidHashFormat_ReturnsFalse()
    {
        // Act
        bool result = _hasher.VerifyPassword("SomePassword", "invalid_hash_string");

        // Assert
        Assert.False(result);
    }
}
