using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Tasks;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Application.Interfaces.Repositories;
using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;
using FluentValidation;

namespace BusinessOperations.Application.Services;

public class TaskItemService : ITaskItemService
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateTaskItemDto> _createValidator;
    private readonly IValidator<UpdateTaskItemDto> _updateValidator;

    public TaskItemService(
        ITaskItemRepository taskRepository,
        IEmployeeRepository employeeRepository,
        IUnitOfWork unitOfWork,
        IValidator<CreateTaskItemDto> createValidator,
        IValidator<UpdateTaskItemDto> updateValidator)
    {
        _taskRepository = taskRepository;
        _employeeRepository = employeeRepository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<TaskItemDto> CreateAsync(Guid currentUserId, CreateTaskItemDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        var creator = await _employeeRepository.GetByUserIdAsync(currentUserId, cancellationToken)
            ?? throw new DomainException("Creator must have an active employee profile to create tasks.");

        if (request.AssignedToEmployeeId.HasValue && request.AssignedToEmployeeId.Value != Guid.Empty)
        {
            var assignee = await _employeeRepository.GetByIdAsync(request.AssignedToEmployeeId.Value, cancellationToken)
                ?? throw new DomainException($"Assigned employee with ID '{request.AssignedToEmployeeId.Value}' was not found.");
        }

        var task = new TaskItem(
            request.Title,
            creator.Id,
            request.Description,
            request.AssignedToEmployeeId,
            request.Priority,
            request.DueDateUtc);

        await _taskRepository.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(task, creator.FullName);
    }

    public async Task<TaskItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken);
        return task == null ? null : MapToDto(task);
    }

    public async Task<PagedResult<TaskItemDto>> GetPagedAsync(
        TaskItemStatus? status,
        TaskPriority? priority,
        Guid? assignedToId,
        Guid? createdById,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var paged = await _taskRepository.GetPagedAsync(status, priority, assignedToId, createdById, pageNumber, pageSize, cancellationToken);
        var dtos = paged.Items.Select(t => MapToDto(t)).ToList();
        return new PagedResult<TaskItemDto>(dtos, paged.TotalCount, paged.PageNumber, paged.PageSize);
    }

    public async Task<TaskItemDto> UpdateAsync(Guid id, Guid currentUserId, UpdateTaskItemDto request, CancellationToken cancellationToken = default)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            throw new DomainException(validation.Errors[0].ErrorMessage);

        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Task not found.");

        if (request.AssignedToEmployeeId.HasValue && request.AssignedToEmployeeId.Value != Guid.Empty)
        {
            var assignee = await _employeeRepository.GetByIdAsync(request.AssignedToEmployeeId.Value, cancellationToken)
                ?? throw new DomainException($"Assigned employee with ID '{request.AssignedToEmployeeId.Value}' was not found.");
        }

        task.UpdateDetails(request.Title, request.Description, request.DueDateUtc);
        task.AssignTo(request.AssignedToEmployeeId);
        task.SetPriority(request.Priority);

        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(task);
    }

    public async Task<TaskItemDto> StartProgressAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Task not found.");

        task.StartProgress();
        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(task);
    }

    public async Task<TaskItemDto> SubmitForReviewAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Task not found.");

        task.SubmitForReview();
        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(task);
    }

    public async Task<TaskItemDto> RequestChangesAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Task not found.");

        task.RequestChanges();
        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(task);
    }

    public async Task<TaskItemDto> CompleteAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Task not found.");

        task.Complete();
        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(task);
    }

    public async Task<TaskItemDto> CancelAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var task = await _taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new DomainException("Task not found.");

        task.Cancel();
        _taskRepository.Update(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(task);
    }

    private static TaskItemDto MapToDto(TaskItem t, string? creatorName = null, string? assigneeName = null)
    {
        return new TaskItemDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            CreatedByEmployeeId = t.CreatedByEmployeeId,
            CreatedByEmployeeName = creatorName ?? t.CreatedByEmployee?.FullName,
            AssignedToEmployeeId = t.AssignedToEmployeeId,
            AssignedToEmployeeName = assigneeName ?? t.AssignedToEmployee?.FullName,
            Priority = t.Priority,
            Status = t.Status,
            DueDateUtc = t.DueDateUtc,
            CompletedAtUtc = t.CompletedAtUtc,
            CreatedAtUtc = t.CreatedAtUtc
        };
    }
}
