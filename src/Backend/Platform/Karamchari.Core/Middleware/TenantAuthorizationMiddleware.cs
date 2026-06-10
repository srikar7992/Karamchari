// -----------------------------------------------------------------------
// <copyright file="TenantAuthorizationMiddleware.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Security.Claims;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Middleware;

/// <summary>
/// Middleware that establishes the <see cref="TenantExecutionContext"/> for the current request.
/// It must run after authentication to have access to the <see cref="ClaimsPrincipal"/>.
/// </summary>
public sealed class TenantAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantAuthorizationMiddleware> _logger;

    public TenantAuthorizationMiddleware(
        RequestDelegate next,
        ILogger<TenantAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        ITenantProvider tenantProvider,
        TenantExecutionContextAccessor contextAccessor)
    {
        // 1. Resolve Tenant Envelope
        if (!tenantProvider.TryGetTenant(out var envelope) || envelope == null)
        {
            // If we can't resolve a tenant, we can't establish a context.
            // ITenantProvider.GetTenant() would have thrown if it were called directly.
            // We allow the request to proceed; downstream authorization policies
            // will catch missing contexts if they require one.
            await _next(context);
            return;
        }

        // 2. Resolve Identity
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            // Anonymous request. We have a tenant but no user.
            // Still establish a context but with minimal identity.
            var anonContext = new TenantExecutionContext(envelope);
            using (contextAccessor.Establish(anonContext))
            {
                await _next(context);
            }
            return;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value
            ?? "anonymous";

        // 3. Resolve Roles and Permissions from Claims
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        var permissions = user.FindAll("permission").Select(c => c.Value).ToHashSet();

        // 4. Establish Canonical Execution Context
        var executionContext = new TenantExecutionContext(
            envelope,
            userId,
            roles,
            permissions);

        using (contextAccessor.Establish(executionContext))
        {
            // 5. Enrichment for Logging/Tracing
            using var scope = _logger.BeginScope(new Dictionary<string, object>
            {
                ["TenantId"] = executionContext.TenantId,
                ["UserId"] = executionContext.UserId,
                ["CorrelationId"] = executionContext.CorrelationId
            });

            await _next(context);
        }
    }
}
