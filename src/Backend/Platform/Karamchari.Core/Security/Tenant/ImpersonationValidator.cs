// -----------------------------------------------------------------------
// <copyright file="ImpersonationValidator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Security.Tenant;

public sealed class ImpersonationValidator
{
    private readonly ILogger<ImpersonationValidator> _logger;
    private readonly ConcurrentDictionary<string, ImpersonationAttempt> _attempts = new();

    public ImpersonationValidator(ILogger<ImpersonationValidator> logger)
    {
        _logger = logger;
    }

    public bool ValidateCrossTenantImpersonation(
        string requestingTenantId,
        string targetTenantId,
        string targetUserId)
    {
        var normalizedRequesting = requestingTenantId.ToLowerInvariant();
        var normalizedTarget = targetTenantId.ToLowerInvariant();

        var isCrossTenant = normalizedRequesting != normalizedTarget;

        var attempt = new ImpersonationAttempt(
            normalizedRequesting,
            normalizedTarget,
            targetUserId,
            DateTime.UtcNow,
            !isCrossTenant);

        _attempts.TryAdd(
            $"{normalizedRequesting}_{normalizedTarget}_{targetUserId}",
            attempt);

        if (isCrossTenant)
        {
            _logger.LogWarning(
                "IMPERSONATION: Cross-tenant impersonation blocked - {RequestingTenantId} attempting to access {TargetTenantId}/{UserId}",
                normalizedRequesting,
                normalizedTarget,
                targetUserId);

            return false;
        }

        _logger.LogDebug(
            "IMPERSONATION: Same-tenant impersonation allowed for {RequestingTenantId} accessing {UserId}",
            normalizedRequesting,
            targetUserId);

        return true;
    }

    public bool ValidateTenantSpecificImpersonation(
        string requestingTenantId,
        string targetUserId,
        IReadOnlyDictionary<string, IReadOnlyList<string>> tenantUsers)
    {
        var normalizedRequesting = requestingTenantId.ToLowerInvariant();

        if (!tenantUsers.TryGetValue(normalizedRequesting, out var allowedUsers))
        {
            _logger.LogWarning(
                "IMPERSONATION: No users found for tenant {TenantId}",
                normalizedRequesting);

            return false;
        }

        var isAllowedUser = allowedUsers.Contains(targetUserId);

        var attempt = new ImpersonationAttempt(
            normalizedRequesting,
            normalizedRequesting,
            targetUserId,
            DateTime.UtcNow,
            isAllowedUser);

        _attempts.TryAdd(
            $"{normalizedRequesting}_{targetUserId}",
            attempt);

        if (!isAllowedUser)
        {
            _logger.LogWarning(
                "IMPERSONATION: User {UserId} not in allowed users for tenant {TenantId}",
                targetUserId,
                normalizedRequesting);
        }

        return isAllowedUser;
    }

    public bool ValidateAdminImpersonation(
        string adminTenantId,
        string targetTenantId,
        string targetUserId,
        bool canImpersonateAnyTenant)
    {
        var normalizedAdmin = adminTenantId.ToLowerInvariant();
        var normalizedTarget = targetTenantId.ToLowerInvariant();

        var isSameTenant = normalizedAdmin == normalizedTarget;
        var canImpersonate = canImpersonateAnyTenant || isSameTenant;

        var attempt = new ImpersonationAttempt(
            normalizedAdmin,
            normalizedTarget,
            targetUserId,
            DateTime.UtcNow,
            canImpersonate,
            true);

        _attempts.TryAdd(
            $"admin_{normalizedAdmin}_{normalizedTarget}_{targetUserId}",
            attempt);

        if (!canImpersonate)
        {
            _logger.LogWarning(
                "IMPERSONATION: Admin impersonation blocked - admin {AdminTenantId} cannot impersonate {TargetTenantId}",
                normalizedAdmin,
                normalizedTarget);
        }

        return canImpersonate;
    }

    public ImpersonationReport GetReport()
    {
        var attempts = _attempts.Values.ToList();

        var totalAttempts = attempts.Count;
        var blockedAttempts = attempts.Count(a => !a.Allowed);
        var allowedAttempts = attempts.Count(a => a.Allowed);

        var crossTenantBlocked = attempts.Count(
            a => a.SourceTenantId != a.TargetTenantId && !a.Allowed);

        return new ImpersonationReport(
            totalAttempts,
            allowedAttempts,
            blockedAttempts,
            crossTenantBlocked);
    }

    public void Clear()
    {
        _attempts.Clear();
    }

    private sealed record ImpersonationAttempt(
        string SourceTenantId,
        string TargetTenantId,
        string TargetUserId,
        DateTime Timestamp,
        bool Allowed,
        bool IsAdmin = false);
}

public sealed record ImpersonationReport(
    int TotalAttempts,
    int AllowedAttempts,
    int BlockedAttempts,
    int CrossTenantBlocked);
