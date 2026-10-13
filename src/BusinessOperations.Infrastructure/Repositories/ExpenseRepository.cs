using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Repositories;

public class ExpenseRepository : IExpenseRepository
{
    private readonly ApplicationDbContext _context;

    public ExpenseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Expense?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .Include(e => e.Employee)
                .ThenInclude(emp => emp!.Department)
            .Include(e => e.ReviewedByEmployee)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Expense>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.Expenses
            .Include(e => e.Employee)
                .ThenInclude(emp => emp!.Department)
            .Include(e => e.ReviewedByEmployee)
            .Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<Expense>> GetPagedAsync(
        ExpenseStatus? status,
        ExpenseCategory? category,
        Guid? employeeId,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Expenses
            .Include(e => e.Employee)
                .ThenInclude(emp => emp!.Department)
            .Include(e => e.ReviewedByEmployee)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(e => e.Status == status.Value);
        }

        if (category.HasValue)
        {
            query = query.Where(e => e.Category == category.Value);
        }

        if (employeeId.HasValue && employeeId.Value != Guid.Empty)
        {
            query = query.Where(e => e.EmployeeId == employeeId.Value);
        }

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(e => e.Employee != null && e.Employee.DepartmentId == departmentId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int validPageNumber = pageNumber < 1 ? 1 : pageNumber;
        int validPageSize = pageSize < 1 ? 10 : pageSize;

        var items = await query
            .OrderByDescending(e => e.ExpenseDate)
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Expense>(items, totalCount, validPageNumber, validPageSize);
    }

    public async Task AddAsync(Expense expense, CancellationToken cancellationToken = default)
    {
        await _context.Expenses.AddAsync(expense, cancellationToken);
    }

    public void Update(Expense expense)
    {
        _context.Expenses.Update(expense);
    }

    public void Delete(Expense expense)
    {
        _context.Expenses.Remove(expense);
    }
}
