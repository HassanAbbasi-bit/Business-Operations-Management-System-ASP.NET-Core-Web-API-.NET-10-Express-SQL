using System.Security.Claims;
using BusinessOperations.Application.DTOs.Auth;
using BusinessOperations.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Registers a new user account with Employee role.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.RegisterAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>
    /// Authenticates a user and issues a signed JWT bearer token.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Returns the currently authenticated user's profile and role claims.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var response = await _authService.GetCurrentUserAsync(userId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Restricted endpoint requiring Admin policy.
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Policy = "RequireAdmin")]
    public IActionResult AdminOnly()
    {
        return Ok(new { Message = "Admin access granted." });
    }

    /// <summary>
    /// Restricted endpoint requiring Manager policy.
    /// </summary>
    [HttpGet("manager-only")]
    [Authorize(Policy = "RequireManager")]
    public IActionResult ManagerOnly()
    {
        return Ok(new { Message = "Manager access granted." });
    }
}
