using System.Security.Claims;
using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Tasks;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskItemService _taskService;

    public TasksController(ITaskItemService taskService)
    {
        _taskService = taskService;
    }

    /// <summary>
    /// Retrieves a paginated list of tasks with optional status, priority, assignee, and creator filters.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResult<TaskItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] TaskItemStatus? status,
        [FromQuery] TaskPriority? priority,
        [FromQuery] Guid? assignedToId,
        [FromQuery] Guid? createdById,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _taskService.GetPagedAsync(status, priority, assignedToId, createdById, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves a single task by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(id, cancellationToken);
        if (task == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Task with ID '{id}' was not found." });

        return Ok(task);
    }

    /// <summary>
    /// Creates a new task assigned to an employee.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateTaskItemDto request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var created = await _taskService.CreateAsync(currentUserId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates details, assignment, and priority of an existing task.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateTaskItemDto request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var updated = await _taskService.UpdateAsync(id, currentUserId.Value, request, cancellationToken);
        return Ok(updated);
    }

    /// <summary>
    /// Transitions task status from Todo to InProgress.
    /// </summary>
    [HttpPost("{id:guid}/start")]
    [Authorize]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartProgress([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var result = await _taskService.StartProgressAsync(id, currentUserId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Transitions task status from InProgress to InReview.
    /// </summary>
    [HttpPost("{id:guid}/review")]
    [Authorize]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitForReview([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var result = await _taskService.SubmitForReviewAsync(id, currentUserId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Transitions task status from InReview back to InProgress (requesting changes).
    /// </summary>
    [HttpPost("{id:guid}/request-changes")]
    [Authorize]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RequestChanges([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var result = await _taskService.RequestChangesAsync(id, currentUserId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Marks a task as complete (Done).
    /// </summary>
    [HttpPost("{id:guid}/complete")]
    [Authorize]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Complete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var result = await _taskService.CompleteAsync(id, currentUserId.Value, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Cancels a task.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(TaskItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var result = await _taskService.CancelAsync(id, currentUserId.Value, cancellationToken);
        return Ok(result);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
