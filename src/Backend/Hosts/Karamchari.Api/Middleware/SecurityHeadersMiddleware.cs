using System.Diagnostics;
using Karamchari.Core.Multitenancy.Execution;

namespace Karamchari.Api.Middleware;

/// <summary>
/// Adds OWASP-recommended security response headers and echoes the request's
/// correlation/trace identifiers (<c>X-Correlation-Id</c>, <c>traceparent</c>) so callers
/// can correlate client-side activity with server logs and traces.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.</summary>
    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Invokes the middleware.</summary>
    public Task InvokeAsync(HttpContext context)
    {
        // Headers must be set before the response starts, including on exception paths.
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            // OWASP secure headers
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["Content-Security-Policy"] =
                "default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'";
            headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";
            headers["Cross-Origin-Embedder-Policy"] = "require-corp";

            // HSTS only over HTTPS (avoid pinning on plain-HTTP dev endpoints).
            if (context.Request.IsHttps)
            {
                headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            }

            // Correlation / trace propagation back to the caller.
            var correlationId = TenantExecutionContext.Current?.CorrelationId
                                ?? Activity.Current?.TraceId.ToString()
                                ?? context.TraceIdentifier;
            headers["X-Correlation-Id"] = correlationId;

            if (Activity.Current is { } activity)
            {
                headers["traceparent"] = activity.Id ?? string.Empty;
            }

            return Task.CompletedTask;
        });

        return _next(context);
    }
}

/// <summary>Extension methods for registering <see cref="SecurityHeadersMiddleware"/>.</summary>
public static class SecurityHeadersMiddlewareExtensions
{
    /// <summary>Adds security and correlation response headers to the pipeline.</summary>
    public static IApplicationBuilder UseKaramchariSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();
}
