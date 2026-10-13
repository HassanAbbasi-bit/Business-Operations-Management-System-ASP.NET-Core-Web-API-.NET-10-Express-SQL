using System.Security.Claims;
using BusinessOperations.Application.DTOs.Dashboard;
using BusinessOperations.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    /// <summary>
    /// Retrieves executive and organizational aggregate metrics (Manager or Admin only).
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Policy = "RequireManager")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Retrieves personalized operational metrics for the current authenticated employee.
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    [ProducesResponseType(typeof(PersonalDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPersonalSummary(CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var personal = await _dashboardService.GetPersonalSummaryAsync(currentUserId.Value, cancellationToken);
        if (personal == null)
            return NotFound(new ProblemDetails { Title = "Not Found", Detail = "Employee profile not found for the current user." });

        return Ok(personal);
    }

    private Guid? GetCurrentUserId()
    {
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
