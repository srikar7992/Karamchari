using FluentAssertions;
using Karamchari.Core.Persistence.Migrations.Tenant;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Migration;

public sealed class MigrationRecoveryEngineTests
{
    private readonly Mock<ILogger<MigrationRecoveryEngine>> _loggerMock;
    private readonly TenantMigrationCheckpoint _checkpoint;
    private readonly MigrationLeaseManager _leaseManager;
    private readonly Mock<TenantMigrationCoordinator> _coordinatorMock;
    private readonly MigrationRecoveryEngine _engine;

    public MigrationRecoveryEngineTests()
    {
        _loggerMock = new Mock<ILogger<MigrationRecoveryEngine>>();
        var stateStore = new InMemoryMigrationStateStore();
        var checkpointLogger = new Mock<ILogger<TenantMigrationCheckpoint>>();
        _checkpoint = new TenantMigrationCheckpoint(stateStore, checkpointLogger.Object);

        var leaseLogger = new Mock<ILogger<MigrationLeaseManager>>();
        _leaseManager = new MigrationLeaseManager(leaseLogger.Object);

        _coordinatorMock = new Mock<TenantMigrationCoordinator>(
            Mock.Of<IMigrationsInvoker>(),
            _leaseManager,
            _checkpoint,
            Mock.Of<ILogger<TenantMigrationCoordinator>>());

        _engine = new MigrationRecoveryEngine(
            _checkpoint,
            _leaseManager,
            _coordinatorMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task DetectInterruptedMigrations_FindsFailedCheckpoints()
    {
        await _checkpoint.SaveCheckpointAsync("acme", new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 3,
            CompletedSteps = new List<string> { "Step1", "Step2", "Step3" },
            HasFailed = true,
            FailureReason = "Test failure"
        });

        var result = await _engine.DetectInterruptedMigrationsAsync(new[] { "acme", "globex" });

        result.InterruptedMigrations.Should().HaveCount(1);
        result.InterruptedMigrations[0].TenantId.Should().Be("acme");
        result.InterruptedMigrations[0].FailureReason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DetectInterruptedMigrations_IgnoresCompletedMigrations()
    {
        await _checkpoint.SaveCheckpointAsync("acme", new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 5,
            CompletedSteps = new List<string> { "Step1", "Step2" }
        });

        var result = await _engine.DetectInterruptedMigrationsAsync(new[] { "acme" });

        result.InterruptedMigrations.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateVersionDrift_SameVersions_Compatible()
    {
        var result = await _engine.ValidateVersionDriftAsync("acme", "v1", "v1");

        result.IsCompatible.Should().BeTrue();
        result.Severity.Should().Be(MigrationSeverity.None);
    }

    [Fact]
    public async Task ValidateVersionDrift_DifferentVersions_Incompatible()
    {
        var result = await _engine.ValidateVersionDriftAsync("acme", "v1", "v2");

        result.IsCompatible.Should().BeFalse();
        result.Severity.Should().Be(MigrationSeverity.High);
    }

    [Fact]
    public async Task PerformRollbackRecovery_NoCheckpoint_ReturnsFailed()
    {
        var result = await _engine.PerformRollbackRecoveryAsync("nonexistent");

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("No checkpoint");
    }

    [Fact]
    public async Task PerformRollbackRecovery_NotFailed_ReturnsFailed()
    {
        await _checkpoint.SaveCheckpointAsync("acme", new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 3,
            CompletedSteps = new List<string> { "Step1", "Step2" },
            HasFailed = false
        });

        var result = await _engine.PerformRollbackRecoveryAsync("acme");

        result.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("not failed");
    }

    [Fact]
    public void AddValidator_AddsSuccessfully()
    {
        var validator = new SchemaIntegrityRecoveryValidator(Mock.Of<ILogger<SchemaIntegrityRecoveryValidator>>());

        _engine.AddValidator(validator);

        _engine.Invoking(e => e.AddValidator(null!))
            .Should().Throw<ArgumentNullException>();
    }
}

public sealed class RecoveryDetectionResultTests
{
    [Fact]
    public void AddInterruptedMigration_AddsToCollection()
    {
        var result = new RecoveryDetectionResult();

        result.AddInterruptedMigration(new InterruptedMigrationInfo
        {
            TenantId = "acme",
            MigrationId = "v1",
            LastCompletedStep = 1,
            InterruptedAt = DateTime.UtcNow,
            Severity = MigrationSeverity.High
        });

        result.InterruptedMigrations.Should().HaveCount(1);
        result.HasInterruptedMigrations.Should().BeTrue();
    }

    [Fact]
    public void AddError_AddsToCollection()
    {
        var result = new RecoveryDetectionResult();

        result.AddError("acme", "Test error");

        result.Errors.Should().ContainKey("acme");
        result.Errors["acme"].Should().Be("Test error");
    }
}

public sealed class VersionDriftResultTests
{
    [Fact]
    public void VersionDriftResult_SetsPropertiesCorrectly()
    {
        var result = new VersionDriftResult
        {
            IsCompatible = true,
            CurrentVersion = "v1",
            ExpectedVersion = "v1",
            Severity = MigrationSeverity.None,
            Message = "Versions compatible"
        };

        result.IsCompatible.Should().BeTrue();
        result.CurrentVersion.Should().Be("v1");
    }
}

public sealed class MigrationSeverityTests
{
    [Theory]
    [InlineData(0, MigrationSeverity.None)]
    [InlineData(1, MigrationSeverity.Low)]
    [InlineData(2, MigrationSeverity.Medium)]
    [InlineData(3, MigrationSeverity.High)]
    [InlineData(4, MigrationSeverity.Critical)]
    public void MigrationSeverity_HasExpectedValues(int expectedValue, MigrationSeverity expected)
    {
        ((int)expected).Should().Be(expectedValue);
    }
}

public sealed class InterruptedMigrationInfoTests
{
    [Fact]
    public void InterruptedMigrationInfo_InitializesCorrectly()
    {
        var info = new InterruptedMigrationInfo
        {
            TenantId = "acme",
            MigrationId = "v1",
            LastCompletedStep = 3,
            InterruptedAt = DateTime.UtcNow,
            FailureReason = "Test failure",
            Severity = MigrationSeverity.High
        };

        info.TenantId.Should().Be("acme");
        info.Severity.Should().Be(MigrationSeverity.High);
    }
}

public sealed class PartialUpgradeResultTests
{
    [Fact]
    public void PartialUpgradeResult_SetsPropertiesCorrectly()
    {
        var result = new PartialUpgradeResult
        {
            Succeeded = true,
            Message = "Can proceed",
            ResumableSteps = 3,
            RemainingSteps = 2,
            RequiresManualIntervention = false
        };

        result.Succeeded.Should().BeTrue();
        result.ResumableSteps.Should().Be(3);
        result.RemainingSteps.Should().Be(2);
    }
}

public sealed class SchemaIntegrityRecoveryValidatorTests
{
    private readonly SchemaIntegrityRecoveryValidator _validator;

    public SchemaIntegrityRecoveryValidatorTests()
    {
        _validator = new SchemaIntegrityRecoveryValidator(
            Mock.Of<ILogger<SchemaIntegrityRecoveryValidator>>());
    }

    [Fact]
    public async Task ValidatePartialUpgradeAsync_ReturnsValid()
    {
        var checkpoint = new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 2,
            CompletedSteps = new List<string> { "Step1", "Step2" }
        };

        var result = await _validator.ValidatePartialUpgradeAsync("acme", checkpoint, CancellationToken.None);

        result.IsValid.Should().BeTrue();
    }
}

public sealed class TenantMigrationCheckpointDataTests
{
    [Fact]
    public void TenantMigrationCheckpointData_InitializesCorrectly()
    {
        var data = new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 5,
            CompletedSteps = new List<string> { "S1", "S2", "S3" },
            SavedAt = DateTime.UtcNow,
            Version = "1.0"
        };

        data.TenantId.Should().Be("acme");
        data.CompletedSteps.Should().HaveCount(3);
    }

    [Fact]
    public void TenantMigrationCheckpointData_WithFailedFlag()
    {
        var data = new TenantMigrationCheckpointData
        {
            TenantId = "acme",
            MigrationId = "v1",
            StartedAt = DateTime.UtcNow,
            LastCompletedStep = 3,
            CompletedSteps = new List<string>(),
            HasFailed = true,
            FailureReason = "Test"
        };

        data.HasFailed.Should().BeTrue();
        data.FailureReason.Should().Be("Test");
    }
}
