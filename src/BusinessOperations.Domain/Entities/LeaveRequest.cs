using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.Domain.Entities;

/// <summary>
/// Leave request submission and review workflow entity.
/// </summary>
public class LeaveRequest : BaseEntity
{
    public Guid EmployeeId { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public LeaveStatus Status { get; private set; } = LeaveStatus.Pending;
    public Guid? ReviewedByEmployeeId { get; private set; }
    public DateTime? ReviewedAtUtc { get; private set; }
    public string? ReviewNotes { get; private set; }

    /// <summary>
    /// Inclusive duration in calendar days.
    /// </summary>
    public int DaysCount => EndDate.DayNumber - StartDate.DayNumber + 1;

    // Navigations
    public virtual Employee? Employee { get; private set; }
    public virtual Employee? ReviewedByEmployee { get; private set; }

    // Parameterless constructor for EF Core
    protected LeaveRequest() { }

    public LeaveRequest(Guid employeeId, DateOnly startDate, DateOnly endDate, string reason)
    {
        if (employeeId == Guid.Empty)
            throw new DomainException("Employee ID cannot be empty.");

        EmployeeId = employeeId;
        SetDates(startDate, endDate);
        SetReason(reason);
        Status = LeaveStatus.Pending;
    }

    public void SetDates(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
            throw new DomainException($"End date '{endDate}' cannot precede start date '{startDate}'.");

        StartDate = startDate;
        EndDate = endDate;
        MarkUpdated();
    }

    public void SetReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Leave reason cannot be empty.");

        Reason = reason.Trim();
        MarkUpdated();
    }

    public void Approve(Guid reviewerEmployeeId, string? reviewNotes = null)
    {
        EnsurePendingState("approve");

        if (reviewerEmployeeId == EmployeeId)
            throw new DomainException("Employees are not permitted to approve their own leave requests.");

        ReviewedByEmployeeId = reviewerEmployeeId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewNotes = reviewNotes?.Trim();
        Status = LeaveStatus.Approved;
        MarkUpdated();
    }

    public void Reject(Guid reviewerEmployeeId, string rejectionReason)
    {
        EnsurePendingState("reject");

        if (reviewerEmployeeId == EmployeeId)
            throw new DomainException("Employees are not permitted to review their own leave requests.");

        if (string.IsNullOrWhiteSpace(rejectionReason))
            throw new DomainException("A rejection reason is required when rejecting a leave request.");

        ReviewedByEmployeeId = reviewerEmployeeId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewNotes = rejectionReason.Trim();
        Status = LeaveStatus.Rejected;
        MarkUpdated();
    }

    public void Cancel(Guid requestingEmployeeId)
    {
        EnsurePendingState("cancel");

        if (requestingEmployeeId != EmployeeId)
            throw new DomainException("Only the employee who submitted the request may cancel it.");

        Status = LeaveStatus.Cancelled;
        MarkUpdated();
    }

    private void EnsurePendingState(string action)
    {
        if (Status != LeaveStatus.Pending)
            throw new DomainException($"Cannot {action} leave request in '{Status}' state. Only pending requests can be transitioned.");
    }
}
