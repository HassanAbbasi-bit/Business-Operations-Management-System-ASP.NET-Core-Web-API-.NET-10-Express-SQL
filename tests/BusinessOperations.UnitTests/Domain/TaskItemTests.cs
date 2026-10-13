using BusinessOperations.Domain.Entities;
using BusinessOperations.Domain.Enums;
using BusinessOperations.Domain.Exceptions;

namespace BusinessOperations.UnitTests.Domain;

public class TaskItemTests
{
    private readonly Guid _creatorId = Guid.NewGuid();
    private readonly Guid _assigneeId = Guid.NewGuid();

    [Fact]
    public void Constructor_WithValidData_InitializesInTodoStatus()
    {
        // Act
        var task = new TaskItem("Design Schema", _creatorId, "Initial DB design", _assigneeId, TaskPriority.High);

        // Assert
        Assert.Equal("Design Schema", task.Title);
        Assert.Equal(_creatorId, task.CreatedByEmployeeId);
        Assert.Equal(_assigneeId, task.AssignedToEmployeeId);
        Assert.Equal(TaskPriority.High, task.Priority);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Null(task.CompletedAtUtc);
    }

    [Fact]
    public void StatusTransitions_FullHappyPath_TransitionsToDone()
    {
        // Arrange
        var task = new TaskItem("Implement Feature", _creatorId);

        // Act & Assert 1: Todo -> InProgress
        task.StartProgress();
        Assert.Equal(TaskItemStatus.InProgress, task.Status);

        // Act & Assert 2: InProgress -> InReview
        task.SubmitForReview();
        Assert.Equal(TaskItemStatus.InReview, task.Status);

        // Act & Assert 3: InReview -> Done
        task.Complete();
        Assert.Equal(TaskItemStatus.Done, task.Status);
        Assert.NotNull(task.CompletedAtUtc);
    }

    [Fact]
    public void StartProgress_WhenAlreadyInProgress_ThrowsDomainException()
    {
        // Arrange
        var task = new TaskItem("Implement Feature", _creatorId);
        task.StartProgress();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => task.StartProgress());
        Assert.Contains("Only tasks in 'Todo' status can be started", ex.Message);
    }

    [Fact]
    public void Cancel_WhenCompleted_ThrowsDomainException()
    {
        // Arrange
        var task = new TaskItem("Implement Feature", _creatorId);
        task.StartProgress();
        task.Complete();

        // Act & Assert
        var ex = Assert.Throws<DomainException>(() => task.Cancel());
        Assert.Contains("Completed tasks cannot be cancelled", ex.Message);
    }
}
