namespace BusinessOperations.Domain.Enums;

/// <summary>
/// Status lifecycle of an expense reimbursement request.
/// </summary>
public enum ExpenseStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Cancelled = 4
}
