using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Expenses;
using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.Interfaces;

public interface IExpenseService
{
    Task<ExpenseDto> SubmitAsync(Guid currentUserId, CreateExpenseDto request, CancellationToken cancellationToken = default);
    Task<ExpenseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseDto>> GetMyExpensesAsync(Guid currentUserId, CancellationToken cancellationToken = default);
    Task<PagedResult<ExpenseDto>> GetPagedAsync(
        ExpenseStatus? status,
        ExpenseCategory? category,
        Guid? employeeId,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<ExpenseDto> ApproveAsync(Guid id, Guid reviewerUserId, ReviewExpenseDto? reviewDto, CancellationToken cancellationToken = default);
    Task<ExpenseDto> RejectAsync(Guid id, Guid reviewerUserId, string rejectionReason, CancellationToken cancellationToken = default);
    Task<ExpenseDto> CancelAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}
