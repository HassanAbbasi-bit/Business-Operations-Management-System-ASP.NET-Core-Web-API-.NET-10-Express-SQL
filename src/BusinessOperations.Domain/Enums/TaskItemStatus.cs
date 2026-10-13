namespace BusinessOperations.Domain.Enums;

/// <summary>
/// Status lifecycle of a task assignment.
/// </summary>
public enum TaskItemStatus
{
    Todo = 1,
    InProgress = 2,
    InReview = 3,
    Done = 4,
    Cancelled = 5
}
