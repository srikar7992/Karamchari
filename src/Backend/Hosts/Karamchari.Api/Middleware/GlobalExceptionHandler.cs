using System.Diagnostics;
using System.Net;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Karamchari.Api.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions into standardized RFC 7807 Problem Details responses.
/// Enriches responses with correlation and trace IDs to facilitate distributed debugging.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionHandler"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var correlationId = TenantExecutionContext.Current?.CorrelationId ?? httpContext.TraceIdentifier;

        _logger.LogError(exception, "Unhandled exception occurred. CorrelationId: {CorrelationId}, Message: {Message}",
            correlationId, exception.Message);

        // SECURITY: never echo raw exception text, SQL messages, or stack traces to clients.
        // The full exception is logged above with the correlation id; clients receive a
        // sanitized, generic message and the correlation id to quote to support.
        var problemDetails = new ProblemDetails
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Server Error",
            Type = "https://karamchari.com/errors/internal-server-error",
            Detail = "An unexpected error occurred while processing the request. "
                   + "Quote the correlationId when contacting support.",
            Instance = httpContext.Request.Path
        };

        // Standardized mapping for specific exception types. Only client-safe details are surfaced.
        if (exception is UnauthorizedAccessException)
        {
            problemDetails.Status = (int)HttpStatusCode.Unauthorized;
            problemDetails.Title = "Unauthorized";
            problemDetails.Type = "https://karamchari.com/errors/unauthorized";
            problemDetails.Detail = "Authentication is required or the supplied credentials are invalid.";
        }
        else if (exception is TenantResolutionException)
        {
            problemDetails.Status = (int)HttpStatusCode.BadRequest;
            problemDetails.Title = "Tenant Resolution Failed";
            problemDetails.Type = "https://karamchari.com/errors/tenant-resolution-failed";
            problemDetails.Detail = "The tenant context for this request could not be resolved.";
        }
        else if (exception is InvalidOperationException)
        {
            problemDetails.Status = (int)HttpStatusCode.BadRequest;
            problemDetails.Title = "Invalid Operation";
            problemDetails.Type = "https://karamchari.com/errors/invalid-operation";
            problemDetails.Detail = exception.Message;
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

        // Enrich with distributed tracing metadata
        problemDetails.Extensions["traceId"] = Activity.Current?.Id ?? httpContext.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = correlationId;

        if (TenantExecutionContext.Current != null)
        {
            problemDetails.Extensions["tenantId"] = TenantExecutionContext.Current.TenantId;
            problemDetails.Extensions["requestId"] = TenantExecutionContext.Current.RequestId;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
