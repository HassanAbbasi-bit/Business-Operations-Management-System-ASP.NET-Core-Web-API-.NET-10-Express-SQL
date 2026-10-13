using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BusinessOperations.Infrastructure.Repositories;

public class TaskItemRepository : ITaskItemRepository
{
    private readonly ApplicationDbContext _context;

    public TaskItemRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .Include(t => t.AssignedToEmployee)
            .Include(t => t.CreatedByEmployee)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<PagedResult<TaskItem>> GetPagedAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        Guid? assignedToId,
        Guid? createdById,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TaskItems
            .Include(t => t.AssignedToEmployee)
            .Include(t => t.CreatedByEmployee)
            .AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }

        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        if (assignedToId.HasValue && assignedToId.Value != Guid.Empty)
        {
            query = query.Where(t => t.AssignedToEmployeeId == assignedToId.Value);
        }

        if (createdById.HasValue && createdById.Value != Guid.Empty)
        {
            query = query.Where(t => t.CreatedByEmployeeId == createdById.Value);
        }

        int totalCount = await query.CountAsync(cancellationToken);
        int validPageNumber = pageNumber < 1 ? 1 : pageNumber;
        int validPageSize = pageSize < 1 ? 10 : pageSize;

        var items = await query
            .OrderByDescending(t => t.CreatedAtUtc)
            .Skip((validPageNumber - 1) * validPageSize)
            .Take(validPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskItem>(items, totalCount, validPageNumber, validPageSize);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken = default)
    {
        await _context.TaskItems.AddAsync(task, cancellationToken);
    }

    public void Update(TaskItem task)
    {
        _context.TaskItems.Update(task);
    }

    public void Delete(TaskItem task)
    {
        _context.TaskItems.Remove(task);
    }
}
