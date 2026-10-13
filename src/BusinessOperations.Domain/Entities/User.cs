using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.Domain.Entities;

/// <summary>
/// Authenticated account credential and role entity.
/// </summary>
public class User : BaseEntity
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; } = UserRole.Employee;
    public bool IsActive { get; private set; } = true;

    // Navigation property: One user maps to an optional employee profile (1-to-0..1)
    public virtual Employee? Employee { get; private set; }

    // Parameterless constructor for EF Core
    protected User() { }

    public User(string email, string passwordHash, UserRole role = UserRole.Employee)
    {
        SetEmail(email);
        SetPasswordHash(passwordHash);
        Role = role;
        IsActive = true;
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty.");

        string trimmed = email.Trim().ToLowerInvariant();
        if (!trimmed.Contains('@') || !trimmed.Contains('.'))
            throw new DomainException($"Email '{email}' is not in a valid format.");

        Email = trimmed;
        MarkUpdated();
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException("Password hash cannot be empty.");

        PasswordHash = passwordHash;
        MarkUpdated();
    }

    public void ChangeRole(UserRole newRole)
    {
        Role = newRole;
        MarkUpdated();
    }

    public void Deactivate()
    {
        IsActive = false;
        MarkUpdated();
    }

    public void Activate()
    {
        IsActive = true;
        MarkUpdated();
    }
}
