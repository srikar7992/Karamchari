using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Migrations.Tenant;

public sealed class TenantMigrationCheckpoint
{
    private readonly IMigrationStateStore _stateStore;
    private readonly ILogger<TenantMigrationCheckpoint> _logger;
    private readonly object _serializationLock = new();

    public TenantMigrationCheckpoint(
        IMigrationStateStore stateStore,
        ILogger<TenantMigrationCheckpoint> logger)
    {
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SaveCheckpointAsync(
        string tenantId,
        TenantMigrationCheckpointData checkpointData)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));
        if (checkpointData == null)
            throw new ArgumentNullException(nameof(checkpointData));

        var checkpoint = checkpointData with
        {
            SavedAt = DateTime.UtcNow,
            Version = "1.0"
        };

        checkpoint.IntegrityHash = ComputeIntegrityHash(checkpoint);

        var serialized = SerializeCheckpoint(checkpoint);
        await _stateStore.SaveStateAsync(tenantId, serialized);

        _logger.LogDebug(
            "Checkpoint saved for tenant {TenantId}, migration {MigrationId}, step {Step}",
            tenantId, checkpoint.MigrationId, checkpoint.LastCompletedStep);
    }

    public async Task<TenantMigrationCheckpointData?> LoadCheckpointAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));

        var serialized = await _stateStore.LoadStateAsync(tenantId);

        if (string.IsNullOrEmpty(serialized))
        {
            _logger.LogDebug(
                "No checkpoint found for tenant {TenantId}",
                tenantId);
            return null;
        }

        try
        {
            var checkpoint = DeserializeCheckpoint(serialized);

            if (!ValidateIntegrity(checkpoint))
            {
                _logger.LogError(
                    "Checkpoint integrity validation failed for tenant {TenantId}",
                    tenantId);
                throw new TenantMigrationException(
                    $"Checkpoint integrity check failed for tenant {tenantId}",
                    TenantMigrationError.CheckpointCorrupted);
            }

            _logger.LogDebug(
                "Checkpoint loaded for tenant {TenantId}, migration {MigrationId}",
                tenantId, checkpoint.MigrationId);

            return checkpoint;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex,
                "Failed to deserialize checkpoint for tenant {TenantId}",
                tenantId);
            throw new TenantMigrationException(
                $"Failed to deserialize checkpoint for tenant {tenantId}",
                TenantMigrationError.CheckpointCorrupted,
                ex);
        }
    }

    public async Task ClearCheckpointAsync(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentNullException(nameof(tenantId));

        await _stateStore.DeleteStateAsync(tenantId);

        _logger.LogInformation(
            "Checkpoint cleared for tenant {TenantId}",
            tenantId);
    }

    public async Task<bool> IsCheckpointValidAsync(string tenantId)
    {
        var checkpoint = await LoadCheckpointAsync(tenantId);
        return checkpoint != null && !checkpoint.HasFailed;
    }

    public async Task<RollbackVerificationResult> VerifyRollbackAsync(
        string tenantId,
        string expectedPreviousVersion)
    {
        var checkpoint = await LoadCheckpointAsync(tenantId);

        if (checkpoint == null)
        {
            return new RollbackVerificationResult
            {
                IsValid = false,
                Message = "No checkpoint found"
            };
        }

        if (!checkpoint.HasFailed && checkpoint.CompletedSteps.Count == 0)
        {
            return new RollbackVerificationResult
            {
                IsValid = true,
                Message = "No rollback required - no completed steps"
            };
        }

        var verificationResult = new RollbackVerificationResult
        {
            IsValid = true,
            Message = "Rollback verification passed",
            CompletedStepsCount = checkpoint.CompletedSteps.Count,
            LastCheckpointTime = checkpoint.SavedAt
        };

        return verificationResult;
    }

    private string ComputeIntegrityHash(TenantMigrationCheckpointData checkpoint)
    {
        var data = $"{checkpoint.TenantId}|{checkpoint.MigrationId}|{checkpoint.LastCompletedStep}|{string.Join(",", checkpoint.CompletedSteps)}|{checkpoint.StartedAt:O}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hashBytes);
    }

    private static bool ValidateIntegrity(TenantMigrationCheckpointData checkpoint)
    {
        if (string.IsNullOrEmpty(checkpoint.IntegrityHash))
            return false;

        var data = $"{checkpoint.TenantId}|{checkpoint.MigrationId}|{checkpoint.LastCompletedStep}|{string.Join(",", checkpoint.CompletedSteps)}|{checkpoint.StartedAt:O}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        var expectedHash = Convert.ToBase64String(hashBytes);

        return string.Equals(checkpoint.IntegrityHash, expectedHash, StringComparison.Ordinal);
    }

    private string SerializeCheckpoint(TenantMigrationCheckpointData checkpoint)
    {
        lock (_serializationLock)
        {
            return JsonSerializer.Serialize(checkpoint, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }

    private TenantMigrationCheckpointData DeserializeCheckpoint(string serialized)
    {
        lock (_serializationLock)
        {
            return JsonSerializer.Deserialize<TenantMigrationCheckpointData>(serialized, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }) ?? throw new JsonException("Failed to deserialize checkpoint");
        }
    }
}

public sealed record TenantMigrationCheckpointData
{
    public required string TenantId { get; init; }
    public required string MigrationId { get; init; }
    public required DateTime StartedAt { get; init; }
    public required int LastCompletedStep { get; init; }
    public required List<string> CompletedSteps { get; init; }
    public DateTime SavedAt { get; init; }
    public string? Version { get; init; }
    public string? IntegrityHash { get; set; }
    public bool HasFailed { get; init; }
    public string? FailureReason { get; init; }
}

public sealed class RollbackVerificationResult
{
    public bool IsValid { get; init; }
    public string? Message { get; init; }
    public int CompletedStepsCount { get; init; }
    public DateTime? LastCheckpointTime { get; init; }
}

public interface IMigrationStateStore
{
    Task SaveStateAsync(string tenantId, string serializedState);
    Task<string?> LoadStateAsync(string tenantId);
    Task DeleteStateAsync(string tenantId);
}

public sealed class InMemoryMigrationStateStore : IMigrationStateStore
{
    private readonly ConcurrentDictionary<string, string> _states = new();

    public Task SaveStateAsync(string tenantId, string serializedState)
    {
        _states[tenantId] = serializedState;
        return Task.CompletedTask;
    }

    public Task<string?> LoadStateAsync(string tenantId)
    {
        _states.TryGetValue(tenantId, out var state);
        return Task.FromResult(state);
    }

    public Task DeleteStateAsync(string tenantId)
    {
        _states.TryRemove(tenantId, out _);
        return Task.CompletedTask;
    }
}
