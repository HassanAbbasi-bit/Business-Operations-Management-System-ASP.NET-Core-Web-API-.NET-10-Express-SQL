using BusinessOperations.Domain.Enums;

namespace BusinessOperations.Application.DTOs.Tasks;

public class UpdateTaskItemDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssignedToEmployeeId { get; set; }
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueDateUtc { get; set; }
}
