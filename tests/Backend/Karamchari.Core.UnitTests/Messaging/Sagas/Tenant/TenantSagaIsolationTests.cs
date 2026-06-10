// -----------------------------------------------------------------------
// <copyright file="TenantSagaIsolationTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.UnitTests.Messaging.Sagas.Tenant;

using FluentAssertions;
using Karamchari.Core.Messaging.Sagas.Tenant;
using Xunit;

public sealed class TenantSagaGuardTests
{
    [Fact]
    public void ValidateTenantOwnership_WhenTenantIdsMatch_ShouldNotThrow()
    {
        var act = () => TenantSagaGuard.ValidateTenantOwnership("tenant-1", "tenant-1");

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateTenantOwnership_WhenTenantIdsDiffer_ShouldThrowCrossTenantException()
    {
        var act = () => TenantSagaGuard.ValidateTenantOwnership("tenant-1", "tenant-2");

        act.Should()
            .Throw<TenantSagaCorrelationException>()
            .Where(ex => ex.ViolationType == TenantCorrelationViolationType.CrossTenantAttempt);
    }

    [Fact]
    public void ValidateTenantOwnership_WhenMessageTenantIdIsNull_ShouldThrowMissingTenantException()
    {
        var act = () => TenantSagaGuard.ValidateTenantOwnership(null!, "tenant-2");

        act.Should()
            .Throw<TenantSagaCorrelationException>()
            .Where(ex => ex.ViolationType == TenantCorrelationViolationType.MissingTenantId);
    }

    [Fact]
    public void ValidateTenantOwnership_WhenSagaTenantIdIsNull_ShouldThrowSagaTenantNotSetException()
    {
        var act = () => TenantSagaGuard.ValidateTenantOwnership("tenant-1", null!);

        act.Should()
            .Throw<TenantSagaCorrelationException>()
            .Where(ex => ex.ViolationType == TenantCorrelationViolationType.SagaTenantNotSet);
    }

    [Fact]
    public void ValidateTenantOwnership_WhenTenantIdsAreCaseDifferent_ShouldNotThrow()
    {
        var act = () => TenantSagaGuard.ValidateTenantOwnership("TENANT-1", "tenant-1");

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateCorrelation_WhenCorrelationIdsMatch_ShouldNotThrow()
    {
        var id = Guid.NewGuid().ToString();
        var act = () => TenantSagaGuard.ValidateCorrelation(id, id);

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateCorrelation_WhenCorrelationIdsDiffer_ShouldThrowException()
    {
        var act = () => TenantSagaGuard.ValidateCorrelation(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString());

        act.Should()
            .Throw<TenantSagaCorrelationException>()
            .Where(ex => ex.ViolationType == TenantCorrelationViolationType.CorrelationIdMismatch);
    }

    [Fact]
    public void IsCrossTenantAttempt_WhenTenantIdsMatch_ShouldReturnFalse()
    {
        var result = TenantSagaGuard.IsCrossTenantAttempt("tenant-1", "tenant-1");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsCrossTenantAttempt_WhenTenantIdsDiffer_ShouldReturnTrue()
    {
        var result = TenantSagaGuard.IsCrossTenantAttempt("tenant-1", "tenant-2");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsCrossTenantAttempt_WhenMessageTenantIdIsNull_ShouldReturnTrue()
    {
        var result = TenantSagaGuard.IsCrossTenantAttempt(null!, "tenant-2");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsCrossTenantAttempt_WhenSagaTenantIdIsNull_ShouldReturnTrue()
    {
        var result = TenantSagaGuard.IsCrossTenantAttempt("tenant-1", null!);

        result.Should().BeTrue();
    }
}

public sealed class TenantSagaCorrelationExceptionTests
{
    [Fact]
    public void ToString_ShouldIncludeAllRelevantDetails()
    {
        var exception = new TenantSagaCorrelationException(
            "Test message",
            "msg-tenant",
            "saga-tenant",
            Guid.NewGuid(),
            Guid.NewGuid(),
            TenantCorrelationViolationType.CrossTenantAttempt);

        var result = exception.ToString();

        result.Should().Contain("Test message");
        result.Should().Contain("msg-tenant");
        result.Should().Contain("saga-tenant");
        result.Should().Contain("CrossTenantAttempt");
    }
}

public sealed class SagaReplayProtectionServiceTests
{
    [Fact]
    public void IsDuplicate_WhenMessageNotProcessed_ShouldReturnFalse()
    {
        var service = CreateService();
        var sagaId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var result = service.IsDuplicate(sagaId, messageId);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsDuplicate_WhenMessageProcessed_ShouldReturnTrue()
    {
        var service = CreateService();
        var sagaId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        service.MarkProcessed(sagaId, messageId);

        var result = service.IsDuplicate(sagaId, messageId);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsStaleCorrelation_WhenSagaIsNew_ShouldReturnFalse()
    {
        var service = CreateService();
        var sagaId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var result = service.IsStaleCorrelation(sagaId, createdAt, TimeSpan.FromHours(1));

        result.Should().BeFalse();
    }

    [Fact]
    public void IsStaleCorrelation_WhenSagaIsOld_ShouldReturnTrue()
    {
        var service = CreateService();
        var sagaId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddHours(-2);

        var result = service.IsStaleCorrelation(sagaId, createdAt, TimeSpan.FromHours(1));

        result.Should().BeTrue();
    }

    [Fact]
    public void IsOrphanedSaga_WhenSagaIsActive_ShouldReturnFalse()
    {
        var service = CreateService();
        var sagaId = Guid.NewGuid();
        var lastActivity = DateTime.UtcNow;

        var result = service.IsOrphanedSaga(sagaId, lastActivity, TimeSpan.FromHours(1));

        result.Should().BeFalse();
    }

    [Fact]
    public void IsOrphanedSaga_WhenSagaHasNoActivity_ShouldReturnTrue()
    {
        var service = CreateService();
        var sagaId = Guid.NewGuid();
        var lastActivity = DateTime.UtcNow.AddHours(-2);

        var result = service.IsOrphanedSaga(sagaId, lastActivity, TimeSpan.FromHours(1));

        result.Should().BeTrue();
    }

    [Fact]
    public void ClearSagaTracking_ShouldRemoveAllTrackingEntries()
    {
        var service = CreateService();
        var sagaId = Guid.NewGuid();
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();

        service.MarkProcessed(sagaId, messageId1);
        service.MarkProcessed(sagaId, messageId2);

        service.ClearSagaTracking(sagaId);

        service.IsDuplicate(sagaId, messageId1).Should().BeFalse();
        service.IsDuplicate(sagaId, messageId2).Should().BeFalse();
    }

    [Fact]
    public void MarkProcessed_ShouldTrackMultipleMessagesPerSaga()
    {
        var service = CreateService();
        var sagaId = Guid.NewGuid();
        var messageId1 = Guid.NewGuid();
        var messageId2 = Guid.NewGuid();

        service.MarkProcessed(sagaId, messageId1);
        service.MarkProcessed(sagaId, messageId2);

        service.IsDuplicate(sagaId, messageId1).Should().BeTrue();
        service.IsDuplicate(sagaId, messageId2).Should().BeTrue();
    }

    private static SagaReplayProtectionService CreateService()
    {
        return new SagaReplayProtectionService(
            Microsoft.Extensions.Logging.Abstractions.NullLogger<SagaReplayProtectionService>.Instance);
    }
}

public sealed class TenantSagaCorrelationResultTests
{
    [Fact]
    public void Success_ShouldHaveIsValidTrue()
    {
        var result = TenantSagaCorrelationResult.Success();

        result.IsValid.Should().BeTrue();
        result.ViolationType.Should().Be(TenantCorrelationViolationType.None);
    }

    [Fact]
    public void Failure_ShouldHaveIsValidFalse()
    {
        var result = TenantSagaCorrelationResult.Failure(
            TenantCorrelationViolationType.CrossTenantAttempt,
            "Test failure",
            "msg-tenant",
            "saga-tenant");

        result.IsValid.Should().BeFalse();
        result.ViolationType.Should().Be(TenantCorrelationViolationType.CrossTenantAttempt);
        result.ErrorMessage.Should().Be("Test failure");
        result.MessageTenantId.Should().Be("msg-tenant");
        result.SagaTenantId.Should().Be("saga-tenant");
    }

    [Fact]
    public void ThrowIfInvalid_WhenResultIsInvalid_ShouldThrow()
    {
        var result = TenantSagaCorrelationResult.Failure(
            TenantCorrelationViolationType.CrossTenantAttempt,
            "Test failure");

        var act = () => result.ThrowIfInvalid();

        act.Should().Throw<TenantSagaCorrelationException>();
    }

    [Fact]
    public void ThrowIfInvalid_WhenResultIsValid_ShouldNotThrow()
    {
        var result = TenantSagaCorrelationResult.Success();

        var act = () => result.ThrowIfInvalid();

        act.Should().NotThrow();
    }
}

public sealed class TenantAwareSagaStateTests
{
    [Fact]
    public void IsCompleted_WhenCompletedAtIsNull_ShouldReturnFalse()
    {
        var state = new TestTenantAwareSagaState { CompletedAt = null };

        state.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void IsCompleted_WhenCompletedAtHasValue_ShouldReturnTrue()
    {
        var state = new TestTenantAwareSagaState { CompletedAt = DateTime.UtcNow };

        state.IsCompleted.Should().BeTrue();
    }

    private sealed class TestTenantAwareSagaState : TenantAwareSagaState
    {
    }
}
