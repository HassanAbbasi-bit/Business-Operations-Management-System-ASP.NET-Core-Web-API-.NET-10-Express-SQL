using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Repositories;

public class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly ApplicationDbContext _context;

    public LeaveRequestRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Include(r => r.ReviewedByEmployee)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<LeaveRequest>> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.LeaveRequests
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Include(r => r.ReviewedByEmployee)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<LeaveRequest>> GetPagedAsync(
        LeaveStatus? status,
        Guid? employeeId,
        Guid? departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRequests
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Include(r => r.ReviewedByEmployee)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        if (employeeId.HasValue && employeeId.Value != Guid.Empty)
        {
            query = query.Where(r => r.EmployeeId == employeeId.Value);
        }

        if (departmentId.HasValue && departmentId.Value != Guid.Empty)
        {
            query = query.Where(r => r.Employee != null && r.Employee.DepartmentId == departmentId.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int validPageNumber = pageNumber < 1 ? 1 : pageNumber;
        int validPageSize = pageSize < 1 ? 10 : pageSize;

        var items = await query
            .OrderByDescending(r => r.CreatedAtUtc)
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<LeaveRequest>(items, totalCount, validPageNumber, validPageSize);
    }

    public async Task<bool> HasActiveOverlappingRequestAsync(
        Guid employeeId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? excludeRequestId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.LeaveRequests
            .Where(r => r.EmployeeId == employeeId)
            .Where(r => r.Status == LeaveStatus.Pending || r.Status == LeaveStatus.Approved)
            .Where(r => r.StartDate <= endDate && r.EndDate >= startDate);

        if (excludeRequestId.HasValue && excludeRequestId.Value != Guid.Empty)
        {
            query = query.Where(r => r.Id != excludeRequestId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(LeaveRequest leaveRequest, CancellationToken cancellationToken = default)
    {
        await _context.LeaveRequests.AddAsync(leaveRequest, cancellationToken);
    }

    public void Update(LeaveRequest leaveRequest)
    {
        _context.LeaveRequests.Update(leaveRequest);
    }
}
