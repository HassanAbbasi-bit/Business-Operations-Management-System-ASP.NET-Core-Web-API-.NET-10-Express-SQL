using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.DTOs.Expenses;

public class ExpenseDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Description { get; set; } = string.Empty;
    public DateOnly ExpenseDate { get; set; }
    public ExpenseStatus Status { get; set; }
    public Guid? ReviewedByEmployeeId { get; set; }
    public string? ReviewedByEmployeeName { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNotes { get; set; }
    public string? RejectionReason => ReviewNotes;
    public DateTime CreatedAtUtc { get; set; }
}
