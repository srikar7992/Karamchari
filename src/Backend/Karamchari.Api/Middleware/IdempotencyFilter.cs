using Karamchari.Core.Domain.Idempotency;
using Karamchari.Core.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Karamchari.Api.Middleware;

public sealed class IdempotencyFilter : IEndpointFilter
{
    private readonly CoreDbContext _dbContext;

    public IdempotencyFilter(CoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var request = context.HttpContext.Request;

        // Only apply to state-changing methods
        if (request.Method != HttpMethods.Post && request.Method != HttpMethods.Put && request.Method != HttpMethods.Delete)
        {
            return await next(context);
        }

        if (!request.Headers.TryGetValue("X-Idempotency-Key", out var idempotencyKey) || string.IsNullOrEmpty(idempotencyKey))
        {
            return await next(context);
        }

        var tenantId = context.HttpContext.User.FindFirst(Karamchari.Core.Multitenancy.TenantConstants.JwtClaimType)?.Value;
        if (string.IsNullOrEmpty(tenantId))
        {
            return Results.Unauthorized();
        }

        // Check if request already processed
        var existingRequest = await _dbContext.IdempotentRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey.ToString() && x.TenantId == tenantId);

        if (existingRequest != null)
        {
            if (existingRequest.IsExpired)
            {
                // Remove expired record and proceed as new request
                _dbContext.IdempotentRequests.Remove(existingRequest);
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                return Results.Json(
                    JsonSerializer.Deserialize<object>(existingRequest.ResponseBody),
                    statusCode: existingRequest.StatusCode);
            }
        }

        // Execute the actual request
        var result = await next(context);

        // Capture and store result if it's a success or client error
        if (result is IResult or object)
        {
            // For foundation, we serialize the result object if possible
            // In a real app, you'd use a more sophisticated way to capture the finalized response
            var statusCode = result switch
            {
                IStatusCodeHttpResult s => s.StatusCode ?? 200,
                _ => 200
            };

            // Only cache "safe" status codes
            if (statusCode is >= 200 and < 300 or >= 400 and < 500)
            {
                var body = JsonSerializer.Serialize(result);
                // 24 hour retention policy
                var ttl = TimeSpan.FromHours(24);
                var idempotentRequest = IdempotentRequest.Create(idempotencyKey.ToString(), tenantId, statusCode, body, ttl);
                
                _dbContext.IdempotentRequests.Add(idempotentRequest);
                await _dbContext.SaveChangesAsync();
            }
        }

        return result;
    }
}

public static class IdempotencyExtensions
{
    public static RouteHandlerBuilder WithIdempotency(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter<IdempotencyFilter>();
    }
}
