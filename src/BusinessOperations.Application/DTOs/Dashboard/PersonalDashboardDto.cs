namespace BusinessOperations.Application.DTOs.Dashboard;

public class PersonalDashboardDto
{
    public Guid EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int AnnualLeaveBalanceDays { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int ApprovedLeaveRequests { get; set; }
    public int AssignedTasksCount { get; set; }
    public int InProgressTasksCount { get; set; }
    public int PendingExpensesCount { get; set; }
    public decimal TotalPendingExpensesAmount { get; set; }
}
