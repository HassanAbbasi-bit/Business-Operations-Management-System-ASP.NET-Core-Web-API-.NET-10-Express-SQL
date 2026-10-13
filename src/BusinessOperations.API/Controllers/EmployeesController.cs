using System.Security.Claims;
using BusinessOperations.Application.Common.Models;
using BusinessOperations.Application.DTOs.Employees;
using BusinessOperations.Application.Interfaces;
using BusinessOperations.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly IEmployeeService _employeeService;

    public EmployeesController(IEmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    /// <summary>
    /// Retrieves a paginated list of employees with optional search and department filtering (Manager or Admin).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(PagedResult<EmployeeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _employeeService.GetPagedAsync(search, departmentId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves an employee profile by ID. Managers/Admins can view any profile; employees can only view their own.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var employee = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (employee == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Employee with ID '{id}' was not found." });

        bool isAdminOrManager = User.IsInRole(UserRole.Admin.ToString()) || User.IsInRole(UserRole.Manager.ToString());
        if (!isAdminOrManager)
        {
            var currentUserId = GetCurrentUserId();
            if (!currentUserId.HasValue || employee.UserId != currentUserId.Value)
            {
                return Forbid();
            }
        }

        return Ok(employee);
    }

    /// <summary>
    /// Creates a new employee profile (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDto request, CancellationToken cancellationToken)
    {
        var created = await _employeeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an employee profile. Admins can update all fields; employees can only update their own names/email.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateEmployeeDto request, CancellationToken cancellationToken)
    {
        var existing = await _employeeService.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = $"Employee with ID '{id}' was not found." });

        bool isAdmin = User.IsInRole(UserRole.Admin.ToString());
        var currentUserId = GetCurrentUserId();

        if (!isAdmin)
        {
            if (!currentUserId.HasValue || existing.UserId != currentUserId.Value)
            {
                return Forbid();
            }

            // Restrict non-admins from changing department, manager, status, or leave balance
            request.DepartmentId = existing.DepartmentId;
            request.ManagerId = existing.ManagerId;
            request.IsActive = existing.IsActive;
            request.AnnualLeaveBalanceDays = existing.AnnualLeaveBalanceDays;
        }

        var updated = await _employeeService.UpdateAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
