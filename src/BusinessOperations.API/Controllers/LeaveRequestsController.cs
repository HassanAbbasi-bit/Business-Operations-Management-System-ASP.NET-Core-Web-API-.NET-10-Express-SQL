using System.Security.Claims;
using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.LeaveRequests;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveRequestsController : ControllerBase
{
    private readonly ILeaveRequestService _leaveRequestService;
    private readonly IEmployeeService _employeeService;

    public LeaveRequestsController(
        ILeaveRequestService leaveRequestService,
        IEmployeeService employeeService)
    {
        _leaveRequestService = leaveRequestService;
        _employeeService = employeeService;
    }

    /// <summary>
    /// Submits a new leave request.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Submit([FromBody] CreateLeaveRequestDto request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var created = await _leaveRequestService.SubmitAsync(currentUserId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Retrieves all leave requests submitted by the current authenticated user.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRequests(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var requests = await _leaveRequestService.GetMyRequestsAsync(currentUserId.Value, cancellationToken);
        return Ok(requests);
    }

    /// <summary>
    /// Retrieves a paginated list of leave requests with optional filters (Manager or Admin only).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(PagedResult<LeaveRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] LeaveStatus? status,
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paged = await _leaveRequestService.GetPagedAsync(status, employeeId, departmentId, page, pageSize, cancellationToken);
        return Ok(paged);
    }

    /// <summary>
    /// Retrieves a single leave request by ID. Submitter, Manager, or Admin can view.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var request = await _leaveRequestService.GetByIdAsync(id, cancellationToken);
        if (request == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Leave request with ID '{id}' was not found." });

        bool isAdminOrManager = User.IsInRole(UserRole.Admin.ToString()) || User.IsInRole(UserRole.Manager.ToString());
        if (!isAdminOrManager)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var currentEmployee = await _employeeService.GetByUserIdAsync(currentUserId.Value, cancellationToken);
            if (currentEmployee == null || currentEmployee.Id != request.EmployeeId)
            {
                return Forbid();
            }
        }

        return Ok(request);
    }

    /// <summary>
    /// Approves a pending leave request (Manager or Admin). Self-approval is forbidden.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid id,
        [FromBody] ReviewLeaveRequestDto? reviewDto,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var approved = await _leaveRequestService.ApproveAsync(id, currentUserId.Value, reviewDto, cancellationToken);
        return Ok(approved);
    }

    /// <summary>
    /// Rejects a pending leave request with a required reason (Manager or Admin). Self-review is forbidden.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] ReviewLeaveRequestDto reviewDto,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var rejected = await _leaveRequestService.RejectAsync(id, currentUserId.Value, reviewDto.Notes ?? string.Empty, cancellationToken);
        return Ok(rejected);
    }

    /// <summary>
    /// Cancels a pending leave request submitted by the current authenticated user.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(LeaveRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var cancelled = await _leaveRequestService.CancelAsync(id, currentUserId.Value, cancellationToken);
        return Ok(cancelled);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
