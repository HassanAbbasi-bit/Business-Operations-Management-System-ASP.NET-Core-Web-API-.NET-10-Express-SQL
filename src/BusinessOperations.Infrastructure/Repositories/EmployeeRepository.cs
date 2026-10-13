using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _context;

    public EmployeeRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .Include(e => e.User)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.Department)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = email.Trim().ToLowerInvariant();
        return await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Email == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        string normalized = email.Trim().ToLowerInvariant();
        return await _context.Employees
            .AnyAsync(e => e.Email == normalized, cancellationToken);
    }

    public async Task AddAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        await _context.Employees.AddAsync(employee, cancellationToken);
    }

    public void Update(Employee employee)
    {
        _context.Employees.Update(employee);
    }
}
