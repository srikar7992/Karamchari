using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Karamchari.Api.Middleware;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var problemDetails = new ProblemDetails
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Server Error",
            Type = "https://karamchari.com/errors/internal-server-error",
            Detail = exception.Message // In production, consider hiding sensitive details
        };

        // Standardized mapping for specific exception types
        if (exception is UnauthorizedAccessException)
        {
            problemDetails.Status = (int)HttpStatusCode.Unauthorized;
            problemDetails.Title = "Unauthorized";
            problemDetails.Type = "https://karamchari.com/errors/unauthorized";
        }
        else if (exception is InvalidOperationException && exception.Message.Contains("not found"))
        {
            problemDetails.Status = (int)HttpStatusCode.NotFound;
            problemDetails.Title = "Resource Not Found";
            problemDetails.Type = "https://karamchari.com/errors/not-found";
        }
        else if (exception is FluentValidation.ValidationException validationEx)
        {
            problemDetails.Status = (int)HttpStatusCode.BadRequest;
            problemDetails.Title = "Validation Error";
            problemDetails.Type = "https://karamchari.com/errors/validation-error";
            problemDetails.Extensions["errors"] = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
