using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.Domain.Entities;

/// <summary>
/// Employee profile representing an individual within the organization.
/// </summary>
public class Employee : BaseEntity
{
    public Guid? UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public Guid DepartmentId { get; private set; }
    public Guid? ManagerId { get; private set; }
    public DateOnly HireDate { get; private set; }
    public int AnnualLeaveBalanceDays { get; private set; } = 20;
    public bool IsActive { get; private set; } = true;

    public string FullName => $"{FirstName} {LastName}".Trim();

    // Navigations
    public virtual Department? Department { get; private set; }
    public virtual User? User { get; private set; }
    public virtual Employee? Manager { get; private set; }
    public virtual ICollection<Employee> DirectReports { get; private set; } = new List<Employee>();
    public virtual ICollection<LeaveRequest> LeaveRequests { get; private set; } = new List<LeaveRequest>();
    public virtual ICollection<TaskItem> AssignedTasks { get; private set; } = new List<TaskItem>();
    public virtual ICollection<TaskItem> CreatedTasks { get; private set; } = new List<TaskItem>();
    public virtual ICollection<Expense> Expenses { get; private set; } = new List<Expense>();

    // Parameterless constructor for EF Core
    protected Employee() { }

    public Employee(
        string firstName,
        string lastName,
        string email,
        Guid departmentId,
        DateOnly hireDate,
        Guid? managerId = null,
        Guid? userId = null,
        int annualLeaveBalanceDays = 20)
    {
        SetNames(firstName, lastName);
        SetEmail(email);
        DepartmentId = departmentId;
        HireDate = hireDate;
        AssignManager(managerId);
        UserId = userId;
        
        if (annualLeaveBalanceDays < 0)
            throw new DomainException("Initial leave balance cannot be negative.");
        AnnualLeaveBalanceDays = annualLeaveBalanceDays;
        IsActive = true;
    }

    public void SetNames(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name cannot be empty.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name cannot be empty.");

        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        MarkUpdated();
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

    public void TransferDepartment(Guid newDepartmentId)
    {
        if (newDepartmentId == Guid.Empty)
            throw new DomainException("Department ID cannot be empty.");

        DepartmentId = newDepartmentId;
        MarkUpdated();
    }

    public void AssignManager(Guid? managerId)
    {
        if (managerId.HasValue && managerId.Value == Id)
            throw new DomainException("An employee cannot report to themselves.");

        ManagerId = managerId;
        MarkUpdated();
    }

    public void LinkUserAccount(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new DomainException("User ID cannot be empty.");

        UserId = userId;
        MarkUpdated();
    }

    public void DeductLeaveDays(int days)
    {
        if (days <= 0)
            throw new DomainException("Deduction days must be greater than zero.");

        if (AnnualLeaveBalanceDays < days)
            throw new DomainException($"Insufficient leave balance. Available: {AnnualLeaveBalanceDays}, Requested: {days}.");

        AnnualLeaveBalanceDays -= days;
        MarkUpdated();
    }

    public void CreditLeaveDays(int days)
    {
        if (days <= 0)
            throw new DomainException("Credit days must be greater than zero.");

        AnnualLeaveBalanceDays += days;
        MarkUpdated();
    }

    public void SetLeaveBalance(int days)
    {
        if (days < 0)
            throw new DomainException("Leave balance cannot be negative.");

        AnnualLeaveBalanceDays = days;
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
