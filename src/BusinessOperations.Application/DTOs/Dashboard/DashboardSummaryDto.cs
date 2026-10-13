namespace BusinessOperations.Application.DTOs.Dashboard;

public class DashboardSummaryDto
{
    public int TotalEmployees { get; set; }
    public int TotalDepartments { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int ApprovedLeaveRequests { get; set; }
    public int TotalTasks { get; set; }
    public int TodoTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int InReviewTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int PendingExpensesCount { get; set; }
    public decimal TotalPendingExpensesAmount { get; set; }
    public decimal TotalApprovedExpensesAmount { get; set; }
}
