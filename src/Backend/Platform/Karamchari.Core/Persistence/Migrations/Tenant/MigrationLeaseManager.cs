using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Migrations.Tenant;

public sealed class MigrationLeaseManager
{
    private readonly ConcurrentDictionary<string, LeaseInfo> _leases = new();
    private readonly ILogger<MigrationLeaseManager> _logger;
    private readonly TimeSpan _defaultLeaseTimeout = TimeSpan.FromMinutes(10);

    public MigrationLeaseManager(ILogger<MigrationLeaseManager> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> AcquireLeaseAsync(
        string tenantId,
        string operationId,
        TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentNullException(nameof(operationId));

        var leaseKey = GetLeaseKey(tenantId);
        var effectiveTimeout = timeout > TimeSpan.Zero ? timeout : _defaultLeaseTimeout;

        var startTime = DateTime.UtcNow;
        var deadline = startTime.Add(effectiveTimeout);

        while (DateTime.UtcNow < deadline)
        {
            var leaseInfo = new LeaseInfo
            {
                TenantId = tenantId,
                OperationId = operationId,
                AcquiredAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(effectiveTimeout)
            };

            if (_leases.TryAdd(leaseKey, leaseInfo))
            {
                _logger.LogInformation(
                    "Lease acquired for tenant {TenantId}, operation {OperationId}, expires at {ExpiresAt}",
                    tenantId, operationId, leaseInfo.ExpiresAt);
                return true;
            }

            if (_leases.TryGetValue(leaseKey, out var existingLease))
            {
                if (existingLease.OperationId == operationId)
                {
                    _logger.LogDebug(
                        "Lease already held for same operation {OperationId}",
                        operationId);
                    return true;
                }

                if (DateTime.UtcNow > existingLease.ExpiresAt)
                {
                    if (_leases.TryRemove(leaseKey, out var removed) &&
                        removed.ExpiresAt == existingLease.ExpiresAt)
                    {
                        if (_leases.TryAdd(leaseKey, leaseInfo))
                        {
                            _logger.LogInformation(
                                "Lease acquired after expiration for tenant {TenantId}, operation {OperationId}",
                                tenantId, operationId);
                            return true;
                        }
                    }
                }
            }

            await Task.Delay(100);
        }

        _logger.LogWarning(
            "Failed to acquire lease for tenant {TenantId}, operation {OperationId} within {Timeout}",
            tenantId, operationId, effectiveTimeout);
        return false;
    }

    public async Task ReleaseLeaseAsync(string tenantId, string operationId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentNullException(nameof(operationId));

        var leaseKey = GetLeaseKey(tenantId);

        if (_leases.TryRemove(leaseKey, out var removedLease))
        {
            if (removedLease.OperationId == operationId)
            {
                _logger.LogInformation(
                    "Lease released for tenant {TenantId}, operation {OperationId}",
                    tenantId, operationId);
                return;
            }

            _leases.TryAdd(leaseKey, removedLease);
            _logger.LogWarning(
                "Lease release rejected for tenant {TenantId}: operation mismatch (expected {Expected}, actual {Actual})",
                tenantId, removedLease.OperationId, operationId);
        }
        else
        {
            _logger.LogDebug(
                "No lease found to release for tenant {TenantId}",
                tenantId);
        }

        await Task.CompletedTask;
    }

    public async Task<bool> ExtendLeaseAsync(
        string tenantId,
        string operationId,
        TimeSpan extension)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(operationId))
            throw new ArgumentNullException(nameof(operationId));

        var leaseKey = GetLeaseKey(tenantId);

        if (!_leases.TryGetValue(leaseKey, out var currentLease))
        {
            _logger.LogWarning(
                "Cannot extend lease for tenant {TenantId}: no lease exists",
                tenantId);
            return false;
        }

        if (currentLease.OperationId != operationId)
        {
            _logger.LogWarning(
                "Cannot extend lease for tenant {TenantId}: operation mismatch",
                tenantId);
            return false;
        }

        var newLease = currentLease with
        {
            ExpiresAt = DateTime.UtcNow.Add(extension)
        };

        if (_leases.TryUpdate(leaseKey, newLease, currentLease))
        {
            _logger.LogInformation(
                "Lease extended for tenant {TenantId}, operation {OperationId}, new expiry {ExpiresAt}",
                tenantId, operationId, newLease.ExpiresAt);
            return true;
        }

        _logger.LogWarning(
            "Failed to extend lease for tenant {TenantId}: concurrent modification",
            tenantId);
        return false;
    }

    public Task<bool> HasLeaseAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));

        var leaseKey = GetLeaseKey(tenantId);

        if (_leases.TryGetValue(leaseKey, out var lease))
        {
            if (DateTime.UtcNow < lease.ExpiresAt)
            {
                return Task.FromResult(true);
            }

            _leases.TryRemove(leaseKey, out _);
        }

        return Task.FromResult(false);
    }

    private static string GetLeaseKey(string tenantId)
    {
        return $"migration:lease:{tenantId}";
    }

    private sealed record LeaseInfo
    {
        public required string TenantId { get; init; }
        public required string OperationId { get; init; }
        public required DateTime AcquiredAt { get; init; }
        public required DateTime ExpiresAt { get; init; }
    }
}
