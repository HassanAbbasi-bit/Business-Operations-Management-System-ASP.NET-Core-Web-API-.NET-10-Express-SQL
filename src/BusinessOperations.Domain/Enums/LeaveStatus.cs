namespace BusinessOperations.Domain.Enums;

/// <summary>
/// Lifecycle states for leave requests.
/// </summary>
public enum LeaveStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
