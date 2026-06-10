// -----------------------------------------------------------------------
// <copyright file="FinancialOperationalPeriodTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.FinancialOps.Domain.Periods;
using Xunit;

namespace Karamchari.FinancialOps.Tests.Domain;

public sealed class FinancialOperationalPeriodTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static FinancialOperationalPeriod CreatePeriod(
        string tenantId = "tenant-1",
        string name = "May 2026 Operations")
    {
        var now = DateTimeOffset.UtcNow;
        return FinancialOperationalPeriod.Create(
            tenantId: tenantId,
            name: name,
            startsAt: now,
            endsAt: now.AddDays(30),
            cutoffAt: now.AddDays(25));
    }

    // ─── Create ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Period_Create_SetsIsLockedFalse()
    {
        var period = CreatePeriod();
        period.IsLocked.Should().BeFalse();
    }

    [Fact]
    public void Period_Create_SetsIsClosedFalse()
    {
        var period = CreatePeriod();
        period.IsClosed.Should().BeFalse();
    }

    [Fact]
    public void Period_Create_PopulatesCoreProperties()
    {
        var now = DateTimeOffset.UtcNow;
        var period = FinancialOperationalPeriod.Create(
            tenantId: "tenant-42",
            name: "June 2026 Operations",
            startsAt: now,
            endsAt: now.AddDays(30),
            cutoffAt: now.AddDays(28));

        period.TenantId.Should().Be("tenant-42");
        period.Name.Should().Be("June 2026 Operations");
        period.StartsAt.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        period.EndsAt.Should().BeCloseTo(now.AddDays(30), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Period_Create_WhenStartsAtAfterEndsAt_ThrowsArgumentException()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => FinancialOperationalPeriod.Create(
            tenantId: "tenant-1",
            name: "Bad Period",
            startsAt: now.AddDays(30),
            endsAt: now,
            cutoffAt: now.AddDays(15));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*StartsAt must be before EndsAt*");
    }

    [Fact]
    public void Period_Create_WhenCutoffOutsidePeriodRange_ThrowsArgumentException()
    {
        var now = DateTimeOffset.UtcNow;

        var act = () => FinancialOperationalPeriod.Create(
            tenantId: "tenant-1",
            name: "Bad Period",
            startsAt: now,
            endsAt: now.AddDays(30),
            cutoffAt: now.AddDays(45)); // beyond endsAt

        act.Should().Throw<ArgumentException>()
            .WithMessage("*CutoffAt must be within the period range*");
    }

    // ─── Lock ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Period_Lock_SetsIsLockedTrue()
    {
        // Arrange
        var period = CreatePeriod();

        // Act
        period.Lock();

        // Assert
        period.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void Period_Lock_DoesNotClosePeriod()
    {
        // Arrange
        var period = CreatePeriod();

        // Act
        period.Lock();

        // Assert
        period.IsClosed.Should().BeFalse();
    }

    [Fact]
    public void Period_Lock_WhenAlreadyClosed_ThrowsInvalidOperationException()
    {
        // Arrange
        var period = CreatePeriod();
        period.Lock();
        period.Close();

        // Act
        var act = () => period.Lock();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*closed*");
    }

    [Fact]
    public void Period_Lock_CalledTwice_IsIdempotent()
    {
        // Arrange
        var period = CreatePeriod();

        // Act — calling Lock a second time should not throw (IsLocked stays true)
        period.Lock();
        var act = () => period.Lock();

        // Assert
        act.Should().NotThrow();
        period.IsLocked.Should().BeTrue();
    }

    // ─── Close ───────────────────────────────────────────────────────────────────

    [Fact]
    public void Period_Close_WhenLocked_SetsIsClosedTrue()
    {
        // Arrange
        var period = CreatePeriod();
        period.Lock();

        // Act
        period.Close();

        // Assert
        period.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void Period_Close_WhenLocked_IsLockedRemainsTrue()
    {
        // Arrange
        var period = CreatePeriod();
        period.Lock();

        // Act
        period.Close();

        // Assert — locking is retained
        period.IsLocked.Should().BeTrue();
    }

    [Fact]
    public void Period_Close_WhenNotLocked_ThrowsInvalidOperationException()
    {
        // Arrange — fresh period, not yet locked
        var period = CreatePeriod();

        // Act
        var act = () => period.Close();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*locked*");
    }
}
