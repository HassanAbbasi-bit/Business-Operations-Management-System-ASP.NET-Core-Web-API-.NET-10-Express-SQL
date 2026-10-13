using System.Text.Json;
using BusinessOperations.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace BusinessOperations.API.Middleware;

/// <summary>
/// Global exception handling middleware that converts domain and unexpected exceptions
/// into standard RFC 7807 ProblemDetails responses without exposing sensitive internals.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain rule violation on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            await HandleDomainExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private static async Task HandleDomainExceptionAsync(HttpContext context, DomainException exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status400BadRequest;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Domain Rule Violation",
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        var responseJson = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(responseJson);
    }

    private async Task HandleUnhandledExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = _environment.IsDevelopment()
                ? exception.Message
                : "An unexpected error occurred. Please contact system support.",
            Instance = context.Request.Path
        };

        var responseJson = JsonSerializer.Serialize(problemDetails);
        await context.Response.WriteAsync(responseJson);
    }
}
