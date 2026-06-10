// -----------------------------------------------------------------------
// <copyright file="TenantMigrationTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Persistence.Migrations.Tenant;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Migration;

public sealed class MigrationLeaseManagerTests
{
    private readonly Mock<ILogger<MigrationLeaseManager>> _loggerMock;
    private readonly MigrationLeaseManager _manager;

    public MigrationLeaseManagerTests()
    {
        _loggerMock = new Mock<ILogger<MigrationLeaseManager>>();
        _manager = new MigrationLeaseManager(_loggerMock.Object);
    }

    [Fact]
    public async Task AcquireLeaseAsync_FirstAcquisition_ReturnsTrue()
    {
        var tenantId = "acme";
        var operationId = "migration-v1";

        var result = await _manager.AcquireLeaseAsync(tenantId, operationId, TimeSpan.FromSeconds(5));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task AcquireLeaseAsync_DifferentOperationId_ReturnsFalse()
    {
        var tenantId = "acme";
        await _manager.AcquireLeaseAsync(tenantId, "op1", TimeSpan.FromSeconds(30));

        var result = await _manager.AcquireLeaseAsync(tenantId, "op2", TimeSpan.FromMilliseconds(100));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLeaseAsync_SameOperationId_ReturnsTrue()
    {
        var tenantId = "acme";
        var operationId = "migration-v1";

        await _manager.AcquireLeaseAsync(tenantId, operationId, TimeSpan.FromSeconds(30));
        var result = await _manager.AcquireLeaseAsync(tenantId, operationId, TimeSpan.FromSeconds(30));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseLeaseAsync_ReleasesSuccessfully()
    {
        var tenantId = "acme";
        var operationId = "migration-v1";
        await _manager.AcquireLeaseAsync(tenantId, operationId, TimeSpan.FromSeconds(30));

        await _manager.ReleaseLeaseAsync(tenantId, operationId);

        var hasLease = await _manager.HasLeaseAsync(tenantId);
        hasLease.Should().BeFalse();
    }

    [Fact]
    public async Task ExtendLeaseAsync_ExtendsSuccessfully()
    {
        var tenantId = "acme";
        var operationId = "migration-v1";
        await _manager.AcquireLeaseAsync(tenantId, operationId, TimeSpan.FromSeconds(30));

        var result = await _manager.ExtendLeaseAsync(tenantId, operationId, TimeSpan.FromMinutes(5));

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExtendLeaseAsync_WrongOperationId_ReturnsFalse()
    {
        var tenantId = "acme";
        await _manager.AcquireLeaseAsync(tenantId, "op1", TimeSpan.FromSeconds(30));

        var result = await _manager.ExtendLeaseAsync(tenantId, "op2", TimeSpan.FromMinutes(5));

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasLeaseAsync_NoLease_ReturnsFalse()
    {
        var result = await _manager.HasLeaseAsync("nonexistent");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AcquireLeaseAsync_EmptyTenantId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _manager.AcquireLeaseAsync("", "op", TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task ReleaseLeaseAsync_EmptyTenantId_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _manager.ReleaseLeaseAsync("", "op"));
    }
}

public sealed class TenantMigrationCheckpointTests
{
    private readonly Mock<ILogger<TenantMigrationCheckpoint>> _loggerMock;
    private readonly InMemoryMigrationStateStore _stateStore;
    private readonly TenantMigrationCheckpoint _checkpoint;

    public TenantMigrationCheckpointTests()
    {
        _loggerMock = new Mock<ILogger<TenantMigrationCheckpoint>>();
        _stateStore = new InMemoryMigrationStateStore();
        _checkpoint = new TenantMigrationCheckpoint(_stateStore, _loggerMock.Object);
    }

    [Fact]
    public async Task SaveCheckpointAsync_SavesSuccessfully()
    {
        var data = new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 1,
            CompletedSteps = new List<string> { "Step1" }
        };

        await _checkpoint.SaveCheckpointAsync("acme", data);

        var loaded = await _checkpoint.LoadCheckpointAsync("acme");
        loaded.Should().NotBeNull();
        loaded!.MigrationId.Should().Be("v1");
        loaded.LastCompletedStep.Should().Be(1);
    }

    [Fact]
    public async Task LoadCheckpointAsync_NoCheckpoint_ReturnsNull()
    {
        var result = await _checkpoint.LoadCheckpointAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ClearCheckpointAsync_ClearsSuccessfully()
    {
        var data = new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 1,
            CompletedSteps = new List<string>()
        };
        await _checkpoint.SaveCheckpointAsync("acme", data);

        await _checkpoint.ClearCheckpointAsync("acme");

        var result = await _checkpoint.LoadCheckpointAsync("acme");
        result.Should().BeNull();
    }

    [Fact]
    public async Task IsCheckpointValidAsync_WithFailedCheckpoint_ReturnsFalse()
    {
        var data = new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 1,
            CompletedSteps = new List<string>(),
            HasFailed = true,
            FailureReason = "Test failure"
        };
        await _checkpoint.SaveCheckpointAsync("acme", data);

        var result = await _checkpoint.IsCheckpointValidAsync("acme");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyRollbackAsync_NoCheckpoint_ReturnsInvalid()
    {
        var result = await _checkpoint.VerifyRollbackAsync("nonexistent", "v0");

        result.IsValid.Should().BeFalse();
        result.Message.Should().Contain("No checkpoint");
    }

    [Fact]
    public async Task SaveCheckpointAsync_IntegrityHashIsComputed()
    {
        var data = new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 1,
            CompletedSteps = new List<string> { "Step1" }
        };

        await _checkpoint.SaveCheckpointAsync("acme", data);

        var loaded = await _checkpoint.LoadCheckpointAsync("acme");
        loaded!.IntegrityHash.Should().NotBeNullOrEmpty();
    }
}

public sealed class MigrationContextValidatorTests
{
    private readonly Mock<ILogger<MigrationContextValidator>> _loggerMock;
    private readonly MigrationContextValidator _validator;

    public MigrationContextValidatorTests()
    {
        _loggerMock = new Mock<ILogger<MigrationContextValidator>>();
        _validator = new MigrationContextValidator(_loggerMock.Object);
    }

    [Fact]
    public void Validate_SameTenantId_ReturnsSucceeded()
    {
        var result = _validator.Validate("acme", "acme");

        result.IsValid.Should().BeTrue();
        result.ValidatedTenantId.Should().Be("acme");
    }

    [Fact]
    public void Validate_DifferentTenantIds_ReturnsFailed()
    {
        var result = _validator.Validate("acme", "globex");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantExecutionEnvelopeValidationError.CrossTenantViolation);
    }

    [Fact]
    public void Validate_EmptyTenantId_ReturnsFailed()
    {
        var result = _validator.Validate("", "acme");

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantExecutionEnvelopeValidationError.MissingTenantId);
    }

    [Fact]
    public void BlockCrossTenantMigration_SameTenant_ReturnsFalse()
    {
        var result = _validator.BlockCrossTenantMigration("acme", "acme");

        result.Should().BeFalse();
    }

    [Fact]
    public void BlockCrossTenantMigration_DifferentTenants_ReturnsTrue()
    {
        var result = _validator.BlockCrossTenantMigration("acme", "globex");

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateRetry_WithinLimit_ReturnsCanRetryTrue()
    {
        var result = _validator.ValidateRetry("acme", 1);

        result.CanRetry.Should().BeTrue();
        result.RemainingRetries.Should().Be(2);
    }

    [Fact]
    public void ValidateRetry_AtLimit_ReturnsCanRetryFalse()
    {
        _validator.ValidateRetry("acme", 0);
        _validator.ValidateRetry("acme", 1);
        _validator.ValidateRetry("acme", 2);

        var result = _validator.ValidateRetry("acme", 3);

        result.CanRetry.Should().BeFalse();
    }

    [Fact]
    public void ValidateBelongsToTenant_SingleMatchingTenant_ReturnsSucceeded()
    {
        var result = _validator.ValidateBelongsToTenant("acme", new[] { "acme" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateBelongsToTenant_MultipleTenants_ReturnsFailed()
    {
        var result = _validator.ValidateBelongsToTenant("acme", new[] { "acme", "globex" });

        result.IsValid.Should().BeFalse();
        result.Error.Should().Be(TenantExecutionEnvelopeValidationError.InvalidMigrationContext);
    }
}

public sealed class TenantMigrationExceptionTests
{
    [Fact]
    public void WithTenant_ReturnsNewExceptionWithTenant()
    {
        var original = new TenantMigrationException("Test error", TenantMigrationError.LeaseAcquisitionFailed);

        var result = original.WithTenant("acme");

        result.TenantId.Should().Be("acme");
        result.Error.Should().Be(TenantMigrationError.LeaseAcquisitionFailed);
    }

    [Fact]
    public void WithMigration_ReturnsNewExceptionWithMigration()
    {
        var original = new TenantMigrationException("Test error", TenantMigrationError.LeaseAcquisitionFailed);

        var result = original.WithMigration("migration-v1");

        result.MigrationId.Should().Be("migration-v1");
    }

    [Fact]
    public void WithFailedStep_ReturnsNewExceptionWithStep()
    {
        var original = new TenantMigrationException("Test error", TenantMigrationError.LeaseAcquisitionFailed);

        var result = original.WithFailedStep(3);

        result.FailedAtStep.Should().Be(3);
    }

    [Fact]
    public void GetErrorDetails_ReturnsCorrectDetails()
    {
        var original = new TenantMigrationException("Test error", TenantMigrationError.LeaseAcquisitionFailed, "acme", "v1", 2, null);

        var details = original.GetErrorDetails();

        details.TenantId.Should().Be("acme");
        details.MigrationId.Should().Be("v1");
        details.FailedAtStep.Should().Be(2);
        details.Error.Should().Be(TenantMigrationError.LeaseAcquisitionFailed);
    }
}

public sealed class MigrationStateTests
{
    [Fact]
    public void MigrationState_InitializesCorrectly()
    {
        var state = new MigrationState
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            CurrentStep = 0,
            Status = MigrationStatus.InProgress
        };

        state.TenantId.Should().Be("acme");
        state.Status.Should().Be(MigrationStatus.InProgress);
        state.CompletedSteps.Should().BeEmpty();
    }

    [Fact]
    public void MigrationResult_Succeeded_SetsCorrectProperties()
    {
        var result = MigrationResult.Succeeded("acme", "v1");

        result.MigrationSucceeded.Should().BeTrue();
        result.TenantId.Should().Be("acme");
        result.MigrationId.Should().Be("v1");
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MigrationResult_Failed_SetsCorrectProperties()
    {
        var result = MigrationResult.Failed("acme", "v1", "Test error");

        result.MigrationSucceeded.Should().BeFalse();
        result.ErrorMessage.Should().Be("Test error");
    }

    [Fact]
    public void RollbackResult_Succeeded_SetsCorrectProperties()
    {
        var result = RollbackResult.Succeeded();

        result.RollbackSucceeded.Should().BeTrue();
    }

    [Fact]
    public void RollbackResult_Failed_SetsCorrectProperties()
    {
        var result = RollbackResult.Failed("Error occurred");

        result.RollbackSucceeded.Should().BeFalse();
        result.ErrorMessage.Should().Be("Error occurred");
    }
}

public sealed class InMemoryMigrationStateStoreTests
{
    [Fact]
    public async Task SaveAndLoad_WorksCorrectly()
    {
        var store = new InMemoryMigrationStateStore();
        var data = "{\"tenantId\":\"acme\"}";

        await store.SaveStateAsync("acme", data);

        var loaded = await store.LoadStateAsync("acme");
        loaded.Should().Be(data);
    }

    [Fact]
    public async Task DeleteState_RemovesData()
    {
        var store = new InMemoryMigrationStateStore();
        await store.SaveStateAsync("acme", "data");

        await store.DeleteStateAsync("acme");

        var loaded = await store.LoadStateAsync("acme");
        loaded.Should().BeNull();
    }

    [Fact]
    public async Task LoadState_NonExistent_ReturnsNull()
    {
        var store = new InMemoryMigrationStateStore();

        var result = await store.LoadStateAsync("nonexistent");

        result.Should().BeNull();
    }
}
