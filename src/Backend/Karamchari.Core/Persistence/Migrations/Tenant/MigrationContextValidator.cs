using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Migrations.Tenant;

public sealed class MigrationContextValidator
{
    private readonly ILogger<MigrationContextValidator> _logger;
    private readonly HashSet<string> _validatedTenants = new();
    private readonly object _validationLock = new();
    private readonly Dictionary<string, int> _retryCountByTenant = new();
    private readonly int _maxRetries;

    public MigrationContextValidator(
        ILogger<MigrationContextValidator> logger,
        int maxRetries = 3)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetries = maxRetries;
    }

    public MigrationContextValidationResult Validate(
        string tenantId,
        string expectedTenantId,
        bool isCrossTenantOperation = false)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return MigrationContextValidationResult.Failed(
                "Tenant ID is required",
                TenantContextValidationError.MissingTenantId);
        }

        if (string.IsNullOrWhiteSpace(expectedTenantId))
        {
            return MigrationContextValidationResult.Failed(
                "Expected tenant ID is required",
                TenantContextValidationError.MissingTenantId);
        }

        if (!isCrossTenantOperation &&
            !string.Equals(tenantId, expectedTenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Cross-tenant migration attempt detected: migrating {Source} but expected {Expected}",
                tenantId, expectedTenantId);

            return MigrationContextValidationResult.Failed(
                $"Cross-tenant migration blocked: source={tenantId}, expected={expectedTenantId}",
                TenantContextValidationError.CrossTenantViolation,
                tenantId);
        }

        return MigrationContextValidationResult.Succeeded(tenantId);
    }

    public MigrationContextValidationResult ValidateBelongsToTenant(
        string tenantId,
        IEnumerable<string> migrationTenantIds)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return MigrationContextValidationResult.Failed(
                "Tenant ID is required",
                TenantContextValidationError.MissingTenantId);
        }

        var tenantIdList = migrationTenantIds.ToList();

        if (tenantIdList.Count == 0)
        {
            return MigrationContextValidationResult.Failed(
                "No migration tenant IDs provided",
                TenantContextValidationError.InvalidMigrationContext);
        }

        if (tenantIdList.Count > 1)
        {
            _logger.LogWarning(
                "Migration contains multiple tenant IDs: {TenantIds}",
                string.Join(", ", tenantIdList));

            return MigrationContextValidationResult.Failed(
                "Migration spans multiple tenants - not allowed for single-tenant operation",
                TenantContextValidationError.InvalidMigrationContext,
                tenantId);
        }

        var migrationTenantId = tenantIdList.Single();

        if (!string.Equals(tenantId, migrationTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return MigrationContextValidationResult.Failed(
                $"Migration {migrationTenantId} does not belong to tenant {tenantId}",
                TenantContextValidationError.TenantMismatch,
                tenantId);
        }

        return MigrationContextValidationResult.Succeeded(tenantId);
    }

    public bool BlockCrossTenantMigration(string sourceTenantId, string targetTenantId)
    {
        if (string.IsNullOrWhiteSpace(sourceTenantId) || string.IsNullOrWhiteSpace(targetTenantId))
        {
            _logger.LogError(
                "Invalid tenant IDs for cross-tenant migration check: source={Source}, target={Target}",
                sourceTenantId, targetTenantId);
            return true;
        }

        if (!string.Equals(sourceTenantId, targetTenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogCritical(
                "BLOCKED: Cross-tenant migration attempt from {Source} to {Target}",
                sourceTenantId, targetTenantId);

            return true;
        }

        return false;
    }

    public SafeRetryResult ValidateRetry(string tenantId, int currentRetryCount)
    {
        lock (_validationLock)
        {
            if (!_retryCountByTenant.TryGetValue(tenantId, out var previousCount))
            {
                _retryCountByTenant[tenantId] = currentRetryCount;
                return new SafeRetryResult
                {
                    CanRetry = currentRetryCount < _maxRetries,
                    RemainingRetries = _maxRetries - currentRetryCount,
                    ShouldResetRetryCount = false
                };
            }

            if (currentRetryCount > previousCount)
            {
                _retryCountByTenant[tenantId] = currentRetryCount;
            }

            var canRetry = currentRetryCount < _maxRetries;
            var shouldReset = currentRetryCount == 0;

            if (canRetry)
            {
                _logger.LogDebug(
                    "Retry validation passed for tenant {TenantId}: attempt {Attempt}/{MaxRetries}",
                    tenantId, currentRetryCount + 1, _maxRetries);
            }
            else
            {
                _logger.LogWarning(
                    "Max retries exceeded for tenant {TenantId}: {Attempt}/{MaxRetries}",
                    tenantId, currentRetryCount, _maxRetries);
            }

            return new SafeRetryResult
            {
                CanRetry = canRetry,
                RemainingRetries = _maxRetries - currentRetryCount,
                ShouldResetRetryCount = shouldReset
            };
        }
    }

    public void ResetRetryCount(string tenantId)
    {
        lock (_validationLock)
        {
            _retryCountByTenant.Remove(tenantId);
            _validatedTenants.Remove(tenantId);
            _logger.LogDebug("Retry count reset for tenant {TenantId}", tenantId);
        }
    }

    public void RecordTenantValidation(string tenantId)
    {
        lock (_validationLock)
        {
            _validatedTenants.Add(tenantId);
            _logger.LogDebug("Tenant validation recorded for {TenantId}", tenantId);
        }
    }

    public bool IsTenantValidated(string tenantId)
    {
        lock (_validationLock)
        {
            return _validatedTenants.Contains(tenantId);
        }
    }

    public MigrationContextValidationResult ValidateMigrationContext(
        string tenantId,
        string migrationId,
        string expectedTenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return MigrationContextValidationResult.Failed(
                "Migration tenant ID is required",
                TenantContextValidationError.MissingTenantId);
        }

        if (string.IsNullOrWhiteSpace(migrationId))
        {
            return MigrationContextValidationResult.Failed(
                "Migration ID is required",
                TenantContextValidationError.InvalidMigrationContext);
        }

        if (!string.Equals(tenantId, expectedTenantId, StringComparison.OrdinalIgnoreCase))
        {
            return MigrationContextValidationResult.Failed(
                $"Migration {migrationId} belongs to tenant {tenantId} but expected {expectedTenantId}",
                TenantContextValidationError.TenantMismatch,
                tenantId);
        }

        return MigrationContextValidationResult.Succeeded(tenantId);
    }
}

public sealed class MigrationContextValidationResult
{
    public bool IsValid { get; private set; }
    public string? ValidatedTenantId { get; private set; }
    public TenantContextValidationError Error { get; private set; }
    public string? ErrorMessage { get; private set; }

    public static MigrationContextValidationResult Succeeded(string tenantId) => new()
    {
        IsValid = true,
        ValidatedTenantId = tenantId,
        Error = TenantContextValidationError.None
    };

    public static MigrationContextValidationResult Failed(string errorMessage, TenantContextValidationError error, string? tenantId = null) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage,
        Error = error,
        ValidatedTenantId = tenantId
    };

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new TenantMigrationException(
                ErrorMessage ?? "Migration context validation failed",
                TenantMigrationError.CrossTenantViolation);
        }
    }
}

public enum TenantContextValidationError
{
    None,
    MissingTenantId,
    CrossTenantViolation,
    TenantMismatch,
    InvalidMigrationContext
}

public sealed class SafeRetryResult
{
    public bool CanRetry { get; init; }
    public int RemainingRetries { get; init; }
    public bool ShouldResetRetryCount { get; init; }
}
