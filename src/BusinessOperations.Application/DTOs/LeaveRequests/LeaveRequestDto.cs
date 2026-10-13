using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.DTOs.LeaveRequests;

public class LeaveRequestDto
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? DepartmentName { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int DaysCount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public LeaveStatus Status { get; set; }
    public Guid? ReviewedByEmployeeId { get; set; }
    public string? ReviewedByEmployeeName { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
