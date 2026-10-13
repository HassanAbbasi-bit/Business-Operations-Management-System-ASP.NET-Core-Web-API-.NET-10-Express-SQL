using BusinessOperations.Application.Common.Models;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.Interfaces.Repositories;

public interface IExpenseRepository
{
    Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Expense>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<PagedResult<Expense>> GetPagedAsync(
        ExpenseStatus? status,
        ExpenseCategory? category,
        Guid? employeeId,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(Expense expense, CancellationToken cancellationToken = default);
    void Update(Expense expense);
    void Delete(Expense expense);
}
