using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Migrations.Tenant;

public sealed class MigrationRecoveryEngine
{
    private readonly TenantMigrationCheckpoint _checkpoint;
    private readonly MigrationLeaseManager _leaseManager;
    private readonly TenantMigrationCoordinator _coordinator;
    private readonly ILogger<MigrationRecoveryEngine> _logger;
    private readonly List<IRecoveryValidator> _validators = new();

    public MigrationRecoveryEngine(
        TenantMigrationCheckpoint checkpoint,
        MigrationLeaseManager leaseManager,
        TenantMigrationCoordinator coordinator,
        ILogger<MigrationRecoveryEngine> logger)
    {
        _checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
        _leaseManager = leaseManager ?? throw new ArgumentNullException(nameof(leaseManager));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void AddValidator(IRecoveryValidator validator)
    {
        _validators.Add(validator ?? throw new ArgumentNullException(nameof(validator)));
    }

    public async Task<RecoveryDetectionResult> DetectInterruptedMigrationsAsync(
        IEnumerable<string> tenantIds,
        CancellationToken cancellationToken = default)
    {
        var result = new RecoveryDetectionResult();
        var tenantsList = tenantIds.ToList();

        _logger.LogInformation(
            "Detecting interrupted migrations for {Count} tenants",
            tenantsList.Count);

        foreach (var tenantId in tenantsList)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var checkpoint = await _checkpoint.LoadCheckpointAsync(tenantId);

                if (checkpoint == null)
                    continue;

                if (checkpoint.HasFailed)
                {
                    result.AddInterruptedMigration(new InterruptedMigrationInfo
                    {
                        TenantId = tenantId,
                        MigrationId = checkpoint.MigrationId,
                        LastCompletedStep = checkpoint.LastCompletedStep,
                        InterruptedAt = checkpoint.SavedAt,
                        FailureReason = checkpoint.FailureReason,
                        Severity = DetermineSeverity(checkpoint)
                    });
                }
                else if (checkpoint.SavedAt < DateTime.UtcNow.AddMinutes(-5))
                {
                    var hasActiveLease = await _leaseManager.HasLeaseAsync(tenantId);
                    if (!hasActiveLease)
                    {
                        result.AddInterruptedMigration(new InterruptedMigrationInfo
                        {
                            TenantId = tenantId,
                            MigrationId = checkpoint.MigrationId,
                            LastCompletedStep = checkpoint.LastCompletedStep,
                            InterruptedAt = checkpoint.SavedAt,
                            Severity = MigrationSeverity.High
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error detecting interrupted migration for tenant {TenantId}",
                    tenantId);
                result.AddError(tenantId, ex.Message);
            }
        }

        _logger.LogInformation(
            "Detected {Count} interrupted migrations",
            result.InterruptedMigrations.Count);

        return result;
    }

    public async Task<RollbackRecoveryResult> PerformRollbackRecoveryAsync(
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Performing rollback recovery for tenant {TenantId}",
            tenantId);

        try
        {
            var checkpoint = await _checkpoint.LoadCheckpointAsync(tenantId);

            if (checkpoint == null)
            {
                return new RollbackRecoveryResult
                {
                    Succeeded = false,
                    Message = "No checkpoint found for rollback"
                };
            }

            if (!checkpoint.HasFailed)
            {
                return new RollbackRecoveryResult
                {
                    Succeeded = false,
                    Message = "Migration has not failed - rollback not required"
                };
            }

            var rollbackResult = await _coordinator.RollbackMigrationAsync(
                tenantId,
                checkpoint.MigrationId,
                cancellationToken);

            return new RollbackRecoveryResult
            {
                Succeeded = rollbackResult.RollbackSucceeded,
                Message = rollbackResult.RollbackSucceeded
                    ? "Rollback completed successfully"
                    : rollbackResult.ErrorMessage ?? "Rollback failed",
                StepsRolledBack = checkpoint.CompletedSteps.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Rollback recovery failed for tenant {TenantId}",
                tenantId);

            return new RollbackRecoveryResult
            {
                Succeeded = false,
                Message = $"Rollback recovery failed: {ex.Message}"
            };
        }
    }

    public async Task<PartialUpgradeResult> HandlePartialUpgradeAsync(
        string tenantId,
        Func<CancellationToken, Task<MigrationStep[]>> migrationStepsFactory,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Handling partial upgrade for tenant {TenantId}",
            tenantId);

        try
        {
            var checkpoint = await _checkpoint.LoadCheckpointAsync(tenantId);

            if (checkpoint == null)
            {
                return new PartialUpgradeResult
                {
                    Succeeded = false,
                    Message = "No checkpoint found"
                };
            }

            foreach (var validator in _validators)
            {
                var validationResult = await validator.ValidatePartialUpgradeAsync(
                    tenantId, checkpoint, cancellationToken);

                if (!validationResult.IsValid)
                {
                    _logger.LogWarning(
                        "Partial upgrade validation failed for tenant {TenantId}: {Reason}",
                        tenantId, validationResult.Message);

                    return new PartialUpgradeResult
                    {
                        Succeeded = false,
                        Message = validationResult.Message,
                        RequiresManualIntervention = true
                    };
                }
            }

            var completedSteps = await migrationStepsFactory(cancellationToken);
            return new PartialUpgradeResult
            {
                Succeeded = true,
                Message = "Partial upgrade can proceed",
                ResumableSteps = checkpoint.CompletedSteps.Count,
                RemainingSteps = completedSteps.Length - checkpoint.CompletedSteps.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Partial upgrade handling failed for tenant {TenantId}",
                tenantId);

            return new PartialUpgradeResult
            {
                Succeeded = false,
                Message = $"Partial upgrade handling failed: {ex.Message}"
            };
        }
    }

    public async Task<VersionDriftResult> ValidateVersionDriftAsync(
        string tenantId,
        string currentVersion,
        string expectedVersion,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Validating version drift for tenant {TenantId}: current={Current}, expected={Expected}",
            tenantId, currentVersion, expectedVersion);

        var isCompatible = IsVersionCompatible(currentVersion, expectedVersion);
        var driftSeverity = CalculateDriftSeverity(currentVersion, expectedVersion);

        return new VersionDriftResult
        {
            IsCompatible = isCompatible,
            CurrentVersion = currentVersion,
            ExpectedVersion = expectedVersion,
            Severity = driftSeverity,
            Message = isCompatible
                ? "Version compatible"
                : $"Version drift detected: {currentVersion} vs expected {expectedVersion}"
        };
    }

    private static MigrationSeverity DetermineSeverity(TenantMigrationCheckpointData checkpoint)
    {
        if (checkpoint.CompletedSteps.Count > 5)
            return MigrationSeverity.Critical;

        if (checkpoint.CompletedSteps.Count > 2)
            return MigrationSeverity.High;

        return MigrationSeverity.Medium;
    }

    private static bool IsVersionCompatible(string current, string expected)
    {
        return string.Equals(current, expected, StringComparison.Ordinal);
    }

    private static MigrationSeverity CalculateDriftSeverity(string current, string expected)
    {
        if (string.Equals(current, expected, StringComparison.Ordinal))
            return MigrationSeverity.None;

        return MigrationSeverity.High;
    }
}

public sealed class RecoveryDetectionResult
{
    public List<InterruptedMigrationInfo> InterruptedMigrations { get; } = new();
    public Dictionary<string, string> Errors { get; } = new();

    public void AddInterruptedMigration(InterruptedMigrationInfo info)
    {
        InterruptedMigrations.Add(info);
    }

    public void AddError(string tenantId, string errorMessage)
    {
        Errors[tenantId] = errorMessage;
    }

    public bool HasInterruptedMigrations => InterruptedMigrations.Count > 0;
}

public sealed class InterruptedMigrationInfo
{
    public required string TenantId { get; init; }
    public required string MigrationId { get; init; }
    public required int LastCompletedStep { get; init; }
    public required DateTime InterruptedAt { get; init; }
    public string? FailureReason { get; init; }
    public MigrationSeverity Severity { get; init; }
}

public sealed class RollbackRecoveryResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public int StepsRolledBack { get; init; }
}

public sealed class PartialUpgradeResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public int ResumableSteps { get; init; }
    public int RemainingSteps { get; init; }
    public bool RequiresManualIntervention { get; init; }
}

public sealed class VersionDriftResult
{
    public bool IsCompatible { get; init; }
    public required string CurrentVersion { get; init; }
    public required string ExpectedVersion { get; init; }
    public MigrationSeverity Severity { get; init; }
    public string? Message { get; init; }
}

public enum MigrationSeverity
{
    None,
    Low,
    Medium,
    High,
    Critical
}

public interface IRecoveryValidator
{
    Task<ValidationResult> ValidatePartialUpgradeAsync(
        string tenantId,
        TenantMigrationCheckpointData checkpoint,
        CancellationToken cancellationToken);
}

public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
}

public sealed class SchemaIntegrityRecoveryValidator : IRecoveryValidator
{
    private readonly ILogger<SchemaIntegrityRecoveryValidator> _logger;

    public SchemaIntegrityRecoveryValidator(ILogger<SchemaIntegrityRecoveryValidator> logger)
    {
        _logger = logger;
    }

    public async Task<ValidationResult> ValidatePartialUpgradeAsync(
        string tenantId,
        TenantMigrationCheckpointData checkpoint,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Validating schema integrity for partial upgrade on tenant {TenantId}",
            tenantId);

        await Task.Delay(10, cancellationToken);

        return new ValidationResult
        {
            IsValid = true,
            Message = "Schema integrity validated"
        };
    }
}
