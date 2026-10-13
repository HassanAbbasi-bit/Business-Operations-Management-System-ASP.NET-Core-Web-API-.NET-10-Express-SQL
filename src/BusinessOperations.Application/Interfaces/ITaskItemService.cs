using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Tasks;
using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.Interfaces;

public interface ITaskItemService
{
    Task<TaskItemDto> CreateAsync(Guid currentUserId, CreateTaskItemDto request, CancellationToken cancellationToken = default);
    Task<TaskItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PagedResult<TaskItemDto>> GetPagedAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        Guid? assignedToId,
        Guid? createdById,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<TaskItemDto> UpdateAsync(Guid id, Guid currentUserId, UpdateTaskItemDto request, CancellationToken cancellationToken = default);
    Task<TaskItemDto> StartProgressAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<TaskItemDto> SubmitForReviewAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<TaskItemDto> RequestChangesAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<TaskItemDto> CompleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
    Task<TaskItemDto> CancelAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default);
}
