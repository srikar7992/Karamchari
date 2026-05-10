using System.Globalization;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Migrations.Tenant;

public sealed class TenantMigrationCoordinator
{
    private readonly IMigrationsInvoker _migrationsInvoker;
    private readonly MigrationLeaseManager _leaseManager;
    private readonly TenantMigrationCheckpoint _checkpoint;
    private readonly ILogger<TenantMigrationCoordinator> _logger;
    private readonly SemaphoreSlim _coordinatorLock = new(1, 1);
    private readonly Dictionary<string, MigrationState> _activeMigrations = new();
    private readonly object _stateLock = new();

    public TenantMigrationCoordinator(
        IMigrationsInvoker migrationsInvoker,
        MigrationLeaseManager leaseManager,
        TenantMigrationCheckpoint checkpoint,
        ILogger<TenantMigrationCoordinator> logger)
    {
        _migrationsInvoker = migrationsInvoker ?? throw new ArgumentNullException(nameof(migrationsInvoker));
        _leaseManager = leaseManager ?? throw new ArgumentNullException(nameof(leaseManager));
        _checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<MigrationResult> ExecuteMigrationAsync(
        string tenantId,
        string migrationId,
        Func<CancellationToken, Task<MigrationStep[]>> migrationStepsFactory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));
        if (string.IsNullOrWhiteSpace(migrationId))
            throw new ArgumentNullException(nameof(migrationId));

        _logger.LogInformation(
            "Starting tenant migration for tenant {TenantId}, migration {MigrationId}",
            tenantId, migrationId);

        var operationId = $"{tenantId}:{migrationId}";

        if (!await _leaseManager.AcquireLeaseAsync(tenantId, operationId, TimeSpan.FromMinutes(5)))
        {
            _logger.LogWarning(
                "Failed to acquire lease for tenant {TenantId}, migration {MigrationId}",
                tenantId, migrationId);
            return MigrationResult.Failed(tenantId, migrationId, "Failed to acquire migration lease");
        }

        try
        {
            await _coordinatorLock.WaitAsync(cancellationToken);

            var existingCheckpoint = await _checkpoint.LoadCheckpointAsync(tenantId);
            if (existingCheckpoint != null && existingCheckpoint.MigrationId == migrationId)
            {
                _logger.LogInformation(
                    "Resuming migration for tenant {TenantId} from checkpoint at step {LastCompletedStep}",
                    tenantId, existingCheckpoint.LastCompletedStep);

                var resumeResult = await ResumeFromCheckpointAsync(
                    tenantId, migrationId, existingCheckpoint, migrationStepsFactory, cancellationToken);
                return resumeResult;
            }

            var migrationState = new MigrationState
            {
                TenantId = tenantId,
                MigrationId = migrationId,
                StartedAt = DateTime.UtcNow,
                CurrentStep = 0,
                Status = MigrationStatus.InProgress
            };

            lock (_stateLock)
            {
                _activeMigrations[tenantId] = migrationState;
            }

            await _checkpoint.SaveCheckpointAsync(tenantId, new TenantMigrationCheckpointData
            {
                TenantId = tenantId,
                MigrationId = migrationId,
                StartedAt = migrationState.StartedAt,
                LastCompletedStep = 0,
                CompletedSteps = new List<string>()
            });

            return await ExecuteMigrationStepsAsync(
                tenantId, migrationId, migrationState, migrationStepsFactory, cancellationToken);
        }
        finally
        {
            await _leaseManager.ReleaseLeaseAsync(tenantId, operationId);
            _coordinatorLock.Release();
        }
    }

    private async Task<MigrationResult> ResumeFromCheckpointAsync(
        string tenantId,
        string migrationId,
        TenantMigrationCheckpointData checkpoint,
        Func<CancellationToken, Task<MigrationStep[]>> migrationStepsFactory,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Resuming tenant migration for {TenantId} from step {Step}",
            tenantId, checkpoint.LastCompletedStep + 1);

        var steps = await migrationStepsFactory(cancellationToken);
        var pendingSteps = steps.Skip(checkpoint.LastCompletedStep).ToArray();

        var completedSteps = new List<string>(checkpoint.CompletedSteps);
        var currentStep = checkpoint.LastCompletedStep;

        foreach (var step in pendingSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogDebug(
                    "Executing step {StepName} for tenant {TenantId}",
                    step.Name, tenantId);

                await step.ExecuteAsync(tenantId, cancellationToken);
                completedSteps.Add(step.Name);
                currentStep++;

                await _checkpoint.SaveCheckpointAsync(tenantId, new TenantMigrationCheckpointData
                {
                    TenantId = tenantId,
                    MigrationId = migrationId,
                    StartedAt = checkpoint.StartedAt,
                    LastCompletedStep = currentStep,
                    CompletedSteps = completedSteps
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Migration step {StepName} failed for tenant {TenantId}",
                    step.Name, tenantId);

                await HandleMigrationFailureAsync(tenantId, migrationId, currentStep, ex);
                return MigrationResult.Failed(tenantId, migrationId, $"Step {step.Name} failed: {ex.Message}");
            }
        }

        await _checkpoint.ClearCheckpointAsync(tenantId);

        _logger.LogInformation(
            "Tenant migration {MigrationId} completed successfully for tenant {TenantId}",
            migrationId, tenantId);

        return MigrationResult.Succeeded(tenantId, migrationId);
    }

    private async Task<MigrationResult> ExecuteMigrationStepsAsync(
        string tenantId,
        string migrationId,
        MigrationState migrationState,
        Func<CancellationToken, Task<MigrationStep[]>> migrationStepsFactory,
        CancellationToken cancellationToken)
    {
        var steps = await migrationStepsFactory(cancellationToken);
        var completedSteps = new List<string>();
        var currentStep = 0;

        try
        {
            foreach (var step in steps)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug(
                    "Executing migration step {StepName} ({Current}/{Total}) for tenant {TenantId}",
                    step.Name, currentStep + 1, steps.Length, tenantId);

                await step.ExecuteAsync(tenantId, cancellationToken);

                completedSteps.Add(step.Name);
                currentStep++;

                migrationState.CurrentStep = currentStep;
                migrationState.CompletedSteps = completedSteps.ToList();

                await _checkpoint.SaveCheckpointAsync(tenantId, new TenantMigrationCheckpointData
                {
                    TenantId = tenantId,
                    MigrationId = migrationId,
                    StartedAt = migrationState.StartedAt,
                    LastCompletedStep = currentStep,
                    CompletedSteps = completedSteps
                });
            }

            await _checkpoint.ClearCheckpointAsync(tenantId);

            lock (_stateLock)
            {
                _activeMigrations.Remove(tenantId);
            }

            _logger.LogInformation(
                "Migration {MigrationId} completed for tenant {TenantId}",
                migrationId, tenantId);

            return MigrationResult.Succeeded(tenantId, migrationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Migration {MigrationId} failed at step {Step} for tenant {TenantId}",
                migrationId, currentStep, tenantId);

            await HandleMigrationFailureAsync(tenantId, migrationId, currentStep, ex);
            return MigrationResult.Failed(tenantId, migrationId, ex.Message);
        }
    }

    private async Task HandleMigrationFailureAsync(
        string tenantId,
        string migrationId,
        int failedAtStep,
        Exception exception)
    {
        _logger.LogWarning(
            "Handling migration failure for tenant {TenantId}, migration {MigrationId} at step {Step}",
            tenantId, migrationId, failedAtStep);

        var state = GetActiveMigrationState(tenantId);
        if (state != null)
        {
            state.Status = MigrationStatus.Failed;
            state.FailedAtStep = failedAtStep;
            state.FailedAt = DateTime.UtcNow;
            state.ErrorMessage = exception.Message;
        }

        await _checkpoint.SaveCheckpointAsync(tenantId, new TenantMigrationCheckpointData
        {
            TenantId = tenantId,
            MigrationId = migrationId,
            StartedAt = state?.StartedAt ?? DateTime.UtcNow,
            LastCompletedStep = failedAtStep - 1,
            CompletedSteps = state?.CompletedSteps ?? new List<string>(),
            HasFailed = true,
            FailureReason = exception.Message
        });
    }

    public async Task<RollbackResult> RollbackMigrationAsync(
        string tenantId,
        string migrationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Initiating rollback for tenant {TenantId}, migration {MigrationId}",
            tenantId, migrationId);

        var operationId = $"{tenantId}:{migrationId}";

        if (!await _leaseManager.AcquireLeaseAsync(tenantId, operationId, TimeSpan.FromMinutes(5)))
        {
            return RollbackResult.Failed("Failed to acquire lease for rollback");
        }

        try
        {
            var checkpoint = await _checkpoint.LoadCheckpointAsync(tenantId);
            if (checkpoint == null)
            {
                return RollbackResult.Failed("No checkpoint found for rollback");
            }

            var rollbackSteps = checkpoint.CompletedSteps.AsEnumerable().Reverse().ToList();
            foreach (var stepName in rollbackSteps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogDebug(
                    "Rolling back step {StepName} for tenant {TenantId}",
                    stepName, tenantId);
            }

            await _checkpoint.ClearCheckpointAsync(tenantId);

            lock (_stateLock)
            {
                _activeMigrations.Remove(tenantId);
            }

            _logger.LogInformation(
                "Rollback completed for tenant {TenantId}, migration {MigrationId}",
                tenantId, migrationId);

            return RollbackResult.Succeeded();
        }
        finally
        {
            await _leaseManager.ReleaseLeaseAsync(tenantId, operationId);
        }
    }

    public MigrationState? GetActiveMigrationState(string tenantId)
    {
        lock (_stateLock)
        {
            return _activeMigrations.TryGetValue(tenantId, out var state) ? state : null;
        }
    }

    public async Task<bool> HasActiveMigrationAsync(string tenantId)
    {
        var state = GetActiveMigrationState(tenantId);
        if (state != null)
            return true;

        var checkpoint = await _checkpoint.LoadCheckpointAsync(tenantId);
        return checkpoint != null && !checkpoint.HasFailed;
    }
}

public sealed class MigrationState
{
    public string TenantId { get; init; } = string.Empty;
    public string MigrationId { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public int CurrentStep { get; set; }
    public MigrationStatus Status { get; set; }
    public List<string> CompletedSteps { get; set; } = new();
    public int? FailedAtStep { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum MigrationStatus
{
    InProgress,
    Completed,
    Failed,
    RolledBack
}

public sealed class MigrationResult
{
    public bool MigrationSucceeded { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string MigrationId { get; private set; } = string.Empty;
    public string? ErrorMessage { get; private set; }
    public DateTime CompletedAt { get; private set; }

    public static MigrationResult Succeeded(string tenantId, string migrationId) => new()
    {
        MigrationSucceeded = true,
        TenantId = tenantId,
        MigrationId = migrationId,
        CompletedAt = DateTime.UtcNow
    };

    public static MigrationResult Failed(string tenantId, string migrationId, string errorMessage) => new()
    {
        MigrationSucceeded = false,
        TenantId = tenantId,
        MigrationId = migrationId,
        ErrorMessage = errorMessage,
        CompletedAt = DateTime.UtcNow
    };
}

public sealed class RollbackResult
{
    public bool RollbackSucceeded { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime CompletedAt { get; private set; }

    public static RollbackResult Succeeded() => new()
    {
        RollbackSucceeded = true,
        CompletedAt = DateTime.UtcNow
    };

    public static RollbackResult Failed(string errorMessage) => new()
    {
        RollbackSucceeded = false,
        ErrorMessage = errorMessage,
        CompletedAt = DateTime.UtcNow
    };
}

public abstract class MigrationStep
{
    public abstract string Name { get; }
    public abstract Task ExecuteAsync(string tenantId, CancellationToken cancellationToken);
}

public interface IMigrationsInvoker
{
    Task InvokeMigrationAsync(string tenantId, string migrationName, CancellationToken cancellationToken);
}

public sealed class DefaultMigrationsInvoker : IMigrationsInvoker
{
    private readonly ILogger<DefaultMigrationsInvoker> _logger;

    public DefaultMigrationsInvoker(ILogger<DefaultMigrationsInvoker> logger)
    {
        _logger = logger;
    }

    public async Task InvokeMigrationAsync(string tenantId, string migrationName, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Invoking migration {MigrationName} for tenant {TenantId}",
            migrationName, tenantId);

        await Task.Delay(50, cancellationToken);
    }
}
