using BusinessOperations.Domain.Common;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.Domain.Entities;

/// <summary>
/// Task assigned to an employee with priority and workflow state tracking.
/// </summary>
public class TaskItem : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? AssignedToEmployeeId { get; private set; }
    public Guid CreatedByEmployeeId { get; private set; }
    public TaskPriority Priority { get; private set; } = TaskPriority.Medium;
    public TaskItemStatus Status { get; private set; } = TaskItemStatus.Todo;
    public DateTime? DueDateUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }

    // Navigations
    public virtual Employee? AssignedToEmployee { get; private set; }
    public virtual Employee? CreatedByEmployee { get; private set; }

    // Parameterless constructor for EF Core
    protected TaskItem() { }

    public TaskItem(
        string title,
        Guid createdByEmployeeId,
        string? description = null,
        Guid? assignedToEmployeeId = null,
        TaskPriority priority = TaskPriority.Medium,
        DateTime? dueDateUtc = null)
    {
        if (createdByEmployeeId == Guid.Empty)
            throw new DomainException("Creator employee ID cannot be empty.");

        CreatedByEmployeeId = createdByEmployeeId;
        SetTitle(title);
        Description = description?.Trim();
        AssignedToEmployeeId = assignedToEmployeeId;
        Priority = priority;
        DueDateUtc = dueDateUtc;
        Status = TaskItemStatus.Todo;
    }

    public void SetTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Task title cannot be empty.");

        Title = title.Trim();
        MarkUpdated();
    }

    public void UpdateDetails(string title, string? description, DateTime? dueDateUtc)
    {
        SetTitle(title);
        Description = description?.Trim();
        DueDateUtc = dueDateUtc;
        MarkUpdated();
    }

    public void AssignTo(Guid? employeeId)
    {
        AssignedToEmployeeId = employeeId;
        MarkUpdated();
    }

    public void SetPriority(TaskPriority priority)
    {
        Priority = priority;
        MarkUpdated();
    }

    public void StartProgress()
    {
        if (Status != TaskItemStatus.Todo)
            throw new DomainException($"Cannot start task in '{Status}' state. Only tasks in 'Todo' status can be started.");

        Status = TaskItemStatus.InProgress;
        MarkUpdated();
    }

    public void SubmitForReview()
    {
        if (Status != TaskItemStatus.InProgress)
            throw new DomainException($"Cannot submit task for review from '{Status}' state. Task must be 'InProgress'.");

        Status = TaskItemStatus.InReview;
        MarkUpdated();
    }

    public void RequestChanges()
    {
        if (Status != TaskItemStatus.InReview)
            throw new DomainException($"Cannot request changes on task in '{Status}' state. Task must be 'InReview'.");

        Status = TaskItemStatus.InProgress;
        MarkUpdated();
    }

    public void Complete()
    {
        if (Status == TaskItemStatus.Done)
            throw new DomainException("Task is already completed.");

        if (Status == TaskItemStatus.Cancelled)
            throw new DomainException("Cancelled tasks cannot be marked complete.");

        Status = TaskItemStatus.Done;
        CompletedAtUtc = DateTime.UtcNow;
        MarkUpdated();
    }

    public void Cancel()
    {
        if (Status == TaskItemStatus.Done)
            throw new DomainException("Completed tasks cannot be cancelled.");

        Status = TaskItemStatus.Cancelled;
        MarkUpdated();
    }
}
