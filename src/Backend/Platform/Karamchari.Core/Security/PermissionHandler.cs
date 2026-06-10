// -----------------------------------------------------------------------
// <copyright file="PermissionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy.Execution;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Security;

/// <summary>
/// Authorization handler that validates permissions against the current <see cref="TenantExecutionContext"/>.
/// </summary>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly TenantExecutionContextAccessor _contextAccessor;
    private readonly ILogger<PermissionHandler> _logger;

    public PermissionHandler(
        TenantExecutionContextAccessor contextAccessor,
        ILogger<PermissionHandler> logger)
    {
        _contextAccessor = contextAccessor;
        _logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var executionContext = _contextAccessor.Current;

        if (executionContext == null)
        {
            _logger.LogWarning("Authorization failed: No TenantExecutionContext available.");
            return Task.CompletedTask;
        }

        // SuperAdmin bypasses all permission checks
        if (executionContext.HasRole(Roles.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Admin bypasses all permission checks within their tenant
        if (executionContext.HasRole(Roles.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (executionContext.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "User {UserId} in Tenant {TenantId} denied permission {Permission}",
                executionContext.UserId,
                executionContext.TenantId,
                requirement.Permission);
        }

        return Task.CompletedTask;
    }
}
