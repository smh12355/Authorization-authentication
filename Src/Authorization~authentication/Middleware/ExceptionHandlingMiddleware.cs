using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_authentication.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Request was cancelled by the client");
            context.Response.StatusCode = 499; // Client Closed Request
            await context.Response.WriteAsJsonAsync(new { message = "Request cancelled" });
        }
        catch (InvalidDataException ex)
        {
            _logger.LogWarning(ex, "Invalid request data");
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Bad request", ex.Message);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Unsupported operation");
            await WriteProblemDetails(context, StatusCodes.Status400BadRequest, "Bad request", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            var message = _env.IsDevelopment() ? ex.Message : "An error occurred";
            await WriteProblemDetails(context, StatusCodes.Status500InternalServerError, "Internal server error", message);
        }
    }

    private static async Task WriteProblemDetails(HttpContext context, int statusCode, string title, string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
