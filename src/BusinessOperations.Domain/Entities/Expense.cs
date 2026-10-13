using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.Domain.Entities;

/// <summary>
/// Expense reimbursement submission and approval entity.
/// </summary>
public class Expense : BaseEntity
{
    public Guid EmployeeId { get; private set; }
    public ExpenseCategory Category { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string Description { get; private set; } = string.Empty;
    public DateOnly ExpenseDate { get; private set; }
    public ExpenseStatus Status { get; private set; } = ExpenseStatus.Pending;
    public Guid? ReviewedByEmployeeId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? ReviewNotes { get; private set; }

    // Navigations
    public virtual Employee? Employee { get; private set; }
    public virtual Employee? ReviewedByEmployee { get; private set; }

    // Parameterless constructor for EF Core
    protected Expense() { }

    public Expense(
        Guid employeeId,
        ExpenseCategory category,
        decimal amount,
        string description,
        DateOnly expenseDate,
        string currency = "USD")
    {
        if (employeeId == Guid.Empty)
            throw new DomainException("Employee ID cannot be empty.");

        EmployeeId = employeeId;
        Category = category;
        SetAmount(amount);
        SetDescription(description);
        SetExpenseDate(expenseDate);
        SetCurrency(currency);
        Status = ExpenseStatus.Pending;
    }

    public void SetAmount(decimal amount)
    {
        if (amount <= 0m)
            throw new DomainException("Expense amount must be greater than zero.");

        // Guard against excessive scale/precision issues
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        MarkUpdated();
    }

    public void SetDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException("Expense description cannot be empty.");

        Description = description.Trim();
        MarkUpdated();
    }

    public void SetExpenseDate(DateOnly expenseDate)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (expenseDate > today)
            throw new DomainException($"Expense date '{expenseDate}' cannot be in the future.");

        ExpenseDate = expenseDate;
        MarkUpdated();
    }

    public void SetCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
            throw new DomainException("Currency must be a valid 3-letter ISO code (e.g., 'USD').");

        Currency = currency.Trim().ToUpperInvariant();
        MarkUpdated();
    }

    public void Approve(Guid reviewerEmployeeId, string? reviewNotes = null)
    {
        EnsurePendingState("approve");

        if (reviewerEmployeeId == EmployeeId)
            throw new DomainException("Employees are not permitted to approve their own expense submissions.");

        ReviewedByEmployeeId = reviewerEmployeeId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewNotes = reviewNotes?.Trim();
        Status = ExpenseStatus.Approved;
        MarkUpdated();
    }

    public void Reject(Guid reviewerEmployeeId, string rejectionReason)
    {
        EnsurePendingState("reject");

        if (reviewerEmployeeId == EmployeeId)
            throw new DomainException("Employees are not permitted to review their own expense submissions.");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new DomainException("A rejection reason is required when rejecting an expense submission.");

        ReviewedByEmployeeId = reviewerEmployeeId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewNotes = rejectionReason.Trim();
        Status = ExpenseStatus.Rejected;
        MarkUpdated();
    }

    public void Cancel(Guid requestingEmployeeId)
    {
        EnsurePendingState("cancel");

        if (requestingEmployeeId != EmployeeId)
            throw new DomainException("Only the submitting employee may cancel this expense submission.");

        Status = ExpenseStatus.Cancelled;
        MarkUpdated();
    }

    private void EnsurePendingState(string action)
    {
        if (Status != ExpenseStatus.Pending)
            throw new DomainException($"Cannot {action} expense submission in '{Status}' state. Only pending submissions can be transitioned.");
    }
}
