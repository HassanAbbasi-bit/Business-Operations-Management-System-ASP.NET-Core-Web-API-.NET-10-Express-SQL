using BusinessOperations.Application.Common.Models;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.Interfaces.Repositories;

public interface ITaskItemRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskItem>> GetPagedAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        Guid? assignedToId,
        Guid? createdById,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task AddAsync(TaskItem task, CancellationToken cancellationToken = default);
    void Update(TaskItem task);
    void Delete(TaskItem task);
}
