using BusinessOperations.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.API.Controllers;

/// <summary>
/// Operational health endpoint reporting service and database readiness.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;

    public HealthController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Performs a health check including database connectivity.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        bool databaseConnected;
        try
        {
            databaseConnected = await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            databaseConnected = false;
        }

        var response = new
        {
            Status = databaseConnected ? "Healthy" : "Degraded",
            TimestampUtc = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
            Database = databaseConnected ? "Connected" : "Unreachable"
        };

        return databaseConnected
            ? Ok(response)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, response);
    }
}
