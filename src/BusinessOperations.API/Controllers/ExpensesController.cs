using System.Security.Claims;
using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Expenses;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly IEmployeeService _employeeService;

    public ExpensesController(
        IExpenseService expenseService,
        IEmployeeService employeeService)
    {
        _expenseService = expenseService;
        _employeeService = employeeService;
    }

    /// <summary>
    /// Submits a new expense reimbursement request.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Submit([FromBody] CreateExpenseDto request, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var created = await _expenseService.SubmitAsync(currentUserId.Value, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Retrieves all expense reimbursement requests submitted by the current authenticated user.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyExpenses(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var expenses = await _expenseService.GetMyExpensesAsync(currentUserId.Value, cancellationToken);
        return Ok(expenses);
    }

    /// <summary>
    /// Retrieves a paginated list of expenses with optional status, category, employee, and department filters (Manager or Admin).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(PagedResult<ExpenseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] ExpenseStatus? status,
        [FromQuery] ExpenseCategory? category,
        [FromQuery] Guid? employeeId,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var paged = await _expenseService.GetPagedAsync(status, category, employeeId, departmentId, page, pageSize, cancellationToken);
        return Ok(paged);
    }

    /// <summary>
    /// Retrieves a single expense request by ID. Submitter, Manager, or Admin can view.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var expense = await _expenseService.GetByIdAsync(id, cancellationToken);
        if (expense == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Expense with ID '{id}' was not found." });

        bool isAdminOrManager = User.IsInRole(UserRole.Admin.ToString()) || User.IsInRole(UserRole.Manager.ToString());
        if (!isAdminOrManager)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue)
                return Unauthorized();

            var currentEmployee = await _employeeService.GetByUserIdAsync(currentUserId.Value, cancellationToken);
            if (currentEmployee == null || currentEmployee.Id != expense.EmployeeId)
            {
                return Forbid();
            }
        }

        return Ok(expense);
    }

    /// <summary>
    /// Approves an expense reimbursement (Manager or Admin). Self-approval is forbidden.
    /// </summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Approve(
        [FromRoute] Guid id,
        [FromBody] ReviewExpenseDto? reviewDto,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var approved = await _expenseService.ApproveAsync(id, currentUserId.Value, reviewDto, cancellationToken);
        return Ok(approved);
    }

    /// <summary>
    /// Rejects an expense reimbursement with a required reason (Manager or Admin). Self-review is forbidden.
    /// </summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reject(
        [FromRoute] Guid id,
        [FromBody] ReviewExpenseDto reviewDto,
        CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var rejected = await _expenseService.RejectAsync(id, currentUserId.Value, reviewDto.Notes ?? string.Empty, cancellationToken);
        return Ok(rejected);
    }

    /// <summary>
    /// Cancels a pending expense submission by the submitting employee.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(typeof(ExpenseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Cancel([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var cancelled = await _expenseService.CancelAsync(id, currentUserId.Value, cancellationToken);
        return Ok(cancelled);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
