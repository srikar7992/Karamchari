// -----------------------------------------------------------------------
// <copyright file="EmployeeAdvanceTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.FinancialOps.Contracts.Primitives;
using Karamchari.FinancialOps.Domain.Ledger;
using Xunit;

namespace Karamchari.FinancialOps.Tests.Domain;

public sealed class EmployeeAdvanceTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static EmployeeAdvance CreateAdvance(
        string tenantId = "tenant-1",
        decimal amount = 5_000m)
    {
        return EmployeeAdvance.Create(
            tenantId: tenantId,
            employeeId: new EmployeeId(Guid.NewGuid()),
            amount: amount,
            periodId: FinancialOperationalPeriodId.New());
    }

    // ─── Create ──────────────────────────────────────────────────────────────────

    [Fact]
    public void EmployeeAdvance_Create_IsNotRecovered()
    {
        var advance = CreateAdvance();
        advance.IsRecovered.Should().BeFalse();
    }

    [Fact]
    public void EmployeeAdvance_Create_RecoveredAtIsNull()
    {
        var advance = CreateAdvance();
        advance.RecoveredAt.Should().BeNull();
    }

    [Fact]
    public void EmployeeAdvance_Create_PopulatesCoreProperties()
    {
        // Arrange
        var employeeId = new EmployeeId(Guid.NewGuid());
        var periodId = FinancialOperationalPeriodId.New();

        // Act
        var advance = EmployeeAdvance.Create(
            tenantId: "tenant-99",
            employeeId: employeeId,
            amount: 12_000m,
            periodId: periodId);

        // Assert
        advance.TenantId.Should().Be("tenant-99");
        advance.EmployeeId.Should().Be(employeeId);
        advance.Amount.Should().Be(12_000m);
        advance.PeriodId.Should().Be(periodId);
        advance.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void EmployeeAdvance_Create_WithDefaultReplayMetadata_UsesEmpty()
    {
        // Arrange & Act — no replayMetadata passed
        var advance = EmployeeAdvance.Create(
            tenantId: "tenant-1",
            employeeId: new EmployeeId(Guid.NewGuid()),
            amount: 3_000m,
            periodId: FinancialOperationalPeriodId.New());

        // Assert — ReplayMetadata is the Empty sentinel
        advance.ReplayMetadata.Should().NotBeNull();
        advance.ReplayMetadata.SourceSystem.Should().Be("System");
        advance.ReplayMetadata.ReplayAttempt.Should().Be(0);
    }

    // ─── MarkAsRecovered ─────────────────────────────────────────────────────────

    [Fact]
    public void EmployeeAdvance_MarkAsRecovered_SetsIsRecoveredTrue()
    {
        // Arrange
        var advance = CreateAdvance();

        // Act
        advance.MarkAsRecovered();

        // Assert
        advance.IsRecovered.Should().BeTrue();
    }

    [Fact]
    public void EmployeeAdvance_MarkAsRecovered_SetsRecoveredAtTimestamp()
    {
        // Arrange
        var advance = CreateAdvance();
        var before = DateTimeOffset.UtcNow;

        // Act
        advance.MarkAsRecovered();

        // Assert
        advance.RecoveredAt.Should().NotBeNull();
        advance.RecoveredAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void EmployeeAdvance_MarkAsRecovered_CalledTwice_IsIdempotent()
    {
        // Arrange
        var advance = CreateAdvance();

        // Act — calling MarkAsRecovered twice should not throw
        advance.MarkAsRecovered();
        var firstRecoveredAt = advance.RecoveredAt;

        var act = () => advance.MarkAsRecovered();

        // Assert
        act.Should().NotThrow();
        advance.IsRecovered.Should().BeTrue();
    }

    [Fact]
    public void EmployeeAdvance_MarkAsRecovered_AmountUnchanged()
    {
        // Arrange
        var advance = CreateAdvance(amount: 7_500m);

        // Act
        advance.MarkAsRecovered();

        // Assert — marking as recovered does not change the amount
        advance.Amount.Should().Be(7_500m);
    }

    // ─── Full lifecycle ───────────────────────────────────────────────────────────

    [Fact]
    public void EmployeeAdvance_FullLifecycle_CreateToRecovered()
    {
        // Arrange
        var employeeId = new EmployeeId(Guid.NewGuid());
        var periodId = FinancialOperationalPeriodId.New();

        var advance = EmployeeAdvance.Create(
            tenantId: "tenant-enterprise",
            employeeId: employeeId,
            amount: 25_000m,
            periodId: periodId);

        // Assert initial state
        advance.IsRecovered.Should().BeFalse();
        advance.RecoveredAt.Should().BeNull();
        advance.Amount.Should().Be(25_000m);
        advance.TenantId.Should().Be("tenant-enterprise");

        // Act — payroll cycle runs and recovers the advance
        advance.MarkAsRecovered();

        // Assert final state
        advance.IsRecovered.Should().BeTrue();
        advance.RecoveredAt.Should().NotBeNull();
        advance.Amount.Should().Be(25_000m); // amount is immutable
    }
}
