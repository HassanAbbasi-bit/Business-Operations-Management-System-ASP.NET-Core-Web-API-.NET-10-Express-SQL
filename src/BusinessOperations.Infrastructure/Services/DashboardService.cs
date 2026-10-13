using BusinessOperations.Application.DTOs.Dashboard;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var totalEmployees = await _context.Employees.CountAsync(e => e.IsActive, cancellationToken);
        var totalDepartments = await _context.Departments.CountAsync(cancellationToken);

        var pendingLeaveRequests = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Pending, cancellationToken);
        var approvedLeaveRequests = await _context.LeaveRequests.CountAsync(l => l.Status == LeaveStatus.Approved, cancellationToken);

        var totalTasks = await _context.TaskItems.CountAsync(cancellationToken);
        var todoTasks = await _context.TaskItems.CountAsync(t => t.Status == TaskItemStatus.Todo, cancellationToken);
        var inProgressTasks = await _context.TaskItems.CountAsync(t => t.Status == TaskItemStatus.InProgress, cancellationToken);
        var inReviewTasks = await _context.TaskItems.CountAsync(t => t.Status == TaskItemStatus.InReview, cancellationToken);
        var completedTasks = await _context.TaskItems.CountAsync(t => t.Status == TaskItemStatus.Done, cancellationToken);

        var pendingExpensesCount = await _context.Expenses.CountAsync(e => e.Status == ExpenseStatus.Pending, cancellationToken);
        var totalPendingExpenses = await _context.Expenses
            .Where(e => e.Status == ExpenseStatus.Pending)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;
        var totalApprovedExpenses = await _context.Expenses
            .Where(e => e.Status == ExpenseStatus.Approved)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        return new DashboardSummaryDto
        {
            TotalEmployees = totalEmployees,
            TotalDepartments = totalDepartments,
            PendingLeaveRequests = pendingLeaveRequests,
            ApprovedLeaveRequests = approvedLeaveRequests,
            TotalTasks = totalTasks,
            TodoTasks = todoTasks,
            InProgressTasks = inProgressTasks,
            InReviewTasks = inReviewTasks,
            CompletedTasks = completedTasks,
            PendingExpensesCount = pendingExpensesCount,
            TotalPendingExpensesAmount = totalPendingExpenses,
            TotalApprovedExpensesAmount = totalApprovedExpenses
        };
    }

    public async Task<PersonalDashboardDto?> GetPersonalSummaryAsync(Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.UserId == currentUserId, cancellationToken);

        if (employee == null)
            return null;

        var pendingLeaves = await _context.LeaveRequests
            .CountAsync(l => l.EmployeeId == employee.Id && l.Status == LeaveStatus.Pending, cancellationToken);

        var approvedLeaves = await _context.LeaveRequests
            .CountAsync(l => l.EmployeeId == employee.Id && l.Status == LeaveStatus.Approved, cancellationToken);

        var assignedTasks = await _context.TaskItems
            .CountAsync(t => t.AssignedToEmployeeId == employee.Id, cancellationToken);

        var inProgressTasks = await _context.TaskItems
            .CountAsync(t => t.AssignedToEmployeeId == employee.Id && t.Status == TaskItemStatus.InProgress, cancellationToken);

        var pendingExpenses = await _context.Expenses
            .CountAsync(e => e.EmployeeId == employee.Id && e.Status == ExpenseStatus.Pending, cancellationToken);

        var pendingExpenseAmount = await _context.Expenses
            .Where(e => e.EmployeeId == employee.Id && e.Status == ExpenseStatus.Pending)
            .SumAsync(e => (decimal?)e.Amount, cancellationToken) ?? 0m;

        return new PersonalDashboardDto
        {
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            DepartmentName = employee.Department?.Name,
            AnnualLeaveBalanceDays = employee.AnnualLeaveBalanceDays,
            PendingLeaveRequests = pendingLeaves,
            ApprovedLeaveRequests = approvedLeaves,
            AssignedTasksCount = assignedTasks,
            InProgressTasksCount = inProgressTasks,
            PendingExpensesCount = pendingExpenses,
            TotalPendingExpensesAmount = pendingExpenseAmount
        };
    }
}
