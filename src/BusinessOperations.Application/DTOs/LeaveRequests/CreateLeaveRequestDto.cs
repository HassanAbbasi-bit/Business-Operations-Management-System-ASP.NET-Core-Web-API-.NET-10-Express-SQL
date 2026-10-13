namespace BusinessOperations.Application.DTOs.LeaveRequests;

public class CreateLeaveRequestDto
{
    public Guid? EmployeeId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}
