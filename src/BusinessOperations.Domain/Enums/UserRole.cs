namespace BusinessOperations.Domain.Enums;

/// <summary>
/// System roles governing authorization boundaries.
/// </summary>
public enum UserRole
{
    /// <summary>
    /// Regular staff member with self-service capabilities.
    /// </summary>
    Employee = 1,

    /// <summary>
    /// Team or department manager with review and assignment capabilities.
    /// </summary>
    Manager = 2,

    /// <summary>
    /// System administrator with global configuration and management privileges.
    /// </summary>
    Admin = 3
}
