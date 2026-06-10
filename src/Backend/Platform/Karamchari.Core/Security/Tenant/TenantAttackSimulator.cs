// -----------------------------------------------------------------------
// <copyright file="TenantAttackSimulator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Security.Tenant;

public sealed class TenantAttackSimulator
{
    private readonly ILogger<TenantAttackSimulator> _logger;
    private readonly ConcurrentDictionary<string, AttackAttempt> _attackAttempts = new();
    private static readonly Random _random = new();

    public TenantAttackSimulator(ILogger<TenantAttackSimulator> logger)
    {
        _logger = logger;
    }

    public async Task SimulateTenantEnumerationAsync(string targetTenantId)
    {
        var normalizedTarget = targetTenantId.ToLowerInvariant();

        var enumerationPatterns = new[]
        {
            "/api/tenants",
            $"/api/tenants/{normalizedTarget}",
            "/api/tenant/list",
            "/api/tenant/search",
            "/api/tenant/all",
            "/api/tenants/active",
            "/api/tenants/find",
            $"/tenants/{normalizedTarget}",
            "/tenants/list",
            $"/tenant/{normalizedTarget}/users"
        };

        _logger.LogWarning(
            "ATTACK: Simulating tenant enumeration attack against {TenantId}",
            normalizedTarget);

        foreach (var pattern in enumerationPatterns)
        {
            var attempt = new AttackAttempt(
                AttackType.TenantEnumeration,
                pattern,
                DateTime.UtcNow,
                false);

            _attackAttempts.TryAdd($"{normalizedTarget}_{pattern}", attempt);
        }

        await Task.Delay(_random.Next(50, 150));

        _logger.LogInformation(
            "ATTACK: Tenant enumeration simulation completed for {TenantId}",
            normalizedTarget);
    }

    public async Task SimulateReplayInjectionAsync(
        string sourceTenantId,
        string targetTenantId,
        IReadOnlyList<string> messageIds)
    {
        var normalizedSource = sourceTenantId.ToLowerInvariant();
        var normalizedTarget = targetTenantId.ToLowerInvariant();

        _logger.LogWarning(
            "ATTACK: Simulating replay injection from {SourceTenantId} to {TargetTenantId}",
            normalizedSource,
            normalizedTarget);

        foreach (var messageId in messageIds)
        {
            var attempt = new AttackAttempt(
                AttackType.ReplayInjection,
                $"message:{messageId}",
                DateTime.UtcNow,
                normalizedSource != normalizedTarget,
                normalizedSource,
                normalizedTarget);

            _attackAttempts.TryAdd($"replay_{normalizedSource}_{normalizedTarget}_{messageId}", attempt);
        }

        await Task.Delay(_random.Next(100, 300));

        _logger.LogInformation(
            "ATTACK: Replay injection simulation completed");
    }

    public async Task SimulateStaleTokenValidationAsync(
        string tenantId,
        TimeSpan tokenAge)
    {
        var normalizedTenant = tenantId.ToLowerInvariant();

        _logger.LogWarning(
            "ATTACK: Simulating stale token validation attack for tenant {TenantId}",
            normalizedTenant);

        var staleTimestamp = DateTime.UtcNow - tokenAge;

        var attempt = new AttackAttempt(
            AttackType.StaleTokenValidation,
            $"token_age:{tokenAge}",
            staleTimestamp,
            tokenAge > TimeSpan.FromHours(1),
            normalizedTenant);

        _attackAttempts.TryAdd($"stale_{normalizedTenant}_{Guid.NewGuid():N}", attempt);

        await Task.Delay(_random.Next(50, 100));

        _logger.LogInformation(
            "ATTACK: Stale token validation simulation completed");
    }

    public async Task SimulateImpersonationAttemptAsync(
        string impersonatorTenantId,
        string targetUserId,
        string targetTenantId)
    {
        var normalizedImpersonator = impersonatorTenantId.ToLowerInvariant();
        var normalizedTarget = targetTenantId.ToLowerInvariant();

        var isCrossTenant = normalizedImpersonator != normalizedTarget;

        _logger.LogWarning(
            "ATTACK: Simulating impersonation attempt by {ImpersonatorTenantId} targeting {UserId} in {TargetTenantId}",
            normalizedImpersonator,
            targetUserId,
            normalizedTarget);

        var attempt = new AttackAttempt(
            AttackType.Impersonation,
            $"user:{targetUserId}",
            DateTime.UtcNow,
            isCrossTenant,
            normalizedImpersonator,
            normalizedTarget);

        _attackAttempts.TryAdd($"impersonate_{normalizedImpersonator}_{normalizedTarget}_{targetUserId}", attempt);

        await Task.Delay(_random.Next(50, 150));

        _logger.LogInformation(
            "ATTACK: Impersonation attempt simulation completed");
    }

    public async Task SimulateForgedHeaderInjectionAsync(
        string tenantId,
        string forgedTenantId)
    {
        var normalizedTenant = tenantId.ToLowerInvariant();
        var normalizedForged = forgedTenantId.ToLowerInvariant();

        var headers = new[]
        {
            "X-Tenant-Id",
            "X-Tenant-ID",
            "x-tenant-id",
            "Tenant-Id",
            "TENANT_ID",
            "x-tenant"
        };

        _logger.LogWarning(
            "ATTACK: Simulating forged header injection for tenant {TenantId}",
            normalizedTenant);

        foreach (var header in headers)
        {
            var attempt = new AttackAttempt(
                AttackType.ForgedHeaderInjection,
                $"{header}:{normalizedForged}",
                DateTime.UtcNow,
                true,
                normalizedTenant);

            _attackAttempts.TryAdd($"header_{normalizedTenant}_{normalizedForged}_{header}", attempt);
        }

        await Task.Delay(_random.Next(50, 150));

        _logger.LogInformation(
            "ATTACK: Forged header injection simulation completed");
    }

    public AttackReport GetAttackReport(string tenantId)
    {
        var normalized = tenantId.ToLowerInvariant();

        var attempts = _attackAttempts
            .Where(kvp => kvp.Value.TenantId == normalized || kvp.Key.Contains(normalized))
            .Select(kvp => kvp.Value)
            .ToList();

        var blockedAttempts = attempts.Count(a => a.Blocked);
        var successfulAttempts = attempts.Count(a => !a.Blocked);

        return new AttackReport(
            normalized,
            attempts.Count,
            blockedAttempts,
            successfulAttempts,
            attempts.GroupBy(a => a.AttackType).ToDictionary(g => g.Key, g => g.Count()));
    }

    public void ClearAttackAttempts()
    {
        _attackAttempts.Clear();
    }

    private sealed record AttackAttempt(
        AttackType AttackType,
        string Details,
        DateTime Timestamp,
        bool Blocked,
        string? SourceTenantId = null,
        string? TargetTenantId = null)
    {
        public string TenantId => SourceTenantId ?? TargetTenantId ?? "unknown";
    }
}

public enum AttackType
{
    TenantEnumeration,
    ReplayInjection,
    StaleTokenValidation,
    Impersonation,
    ForgedHeaderInjection
}

public sealed record AttackReport(
    string TenantId,
    int TotalAttempts,
    int BlockedAttempts,
    int SuccessfulAttempts,
    Dictionary<AttackType, int> AttemptsByType);
