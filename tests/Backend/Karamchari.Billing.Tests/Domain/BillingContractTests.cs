// -----------------------------------------------------------------------
// <copyright file="BillingContractTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Billing.Domain.Contracts;
using Xunit;

namespace Karamchari.Billing.Tests.Domain;

public class BillingContractTests
{
    private static BillingContract CreateContract()
    {
        return BillingContract.Create(
            tenantId: "tenant-1",
            clientId: Guid.NewGuid(),
            projectId: Guid.NewGuid(),
            type: BillingType.TimeAndMaterial);
    }

    [Fact]
    public void BillingContract_AddRate_SetsRateForRole()
    {
        // Arrange
        var contract = CreateContract();
        var roleId = Guid.NewGuid();
        var effectiveFrom = new DateOnly(2025, 1, 1);

        // Act
        contract.AddRate(roleId, rate: 150m, unit: RateUnit.Hour, effectiveFrom: effectiveFrom);

        // Assert
        contract.RateCards.Should().HaveCount(1);
        var card = contract.RateCards.Single();
        card.RoleId.Should().Be(roleId);
        card.Rate.Should().Be(150m);
        card.Unit.Should().Be(RateUnit.Hour);
        card.EffectiveFrom.Should().Be(effectiveFrom);
    }

    [Fact]
    public void BillingContract_AddRate_OverlappingDatesForSameRole_ThrowsException()
    {
        // Arrange
        var contract = CreateContract();
        var roleId = Guid.NewGuid();

        // Add first rate: Jan 1 – Mar 31 2025
        contract.AddRate(roleId, rate: 100m, unit: RateUnit.Hour,
            effectiveFrom: new DateOnly(2025, 1, 1),
            effectiveTo: new DateOnly(2025, 3, 31));

        // Act — try to add overlapping rate: Feb 1 – Apr 30 2025
        var act = () => contract.AddRate(roleId, rate: 120m, unit: RateUnit.Hour,
            effectiveFrom: new DateOnly(2025, 2, 1),
            effectiveTo: new DateOnly(2025, 4, 30));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{roleId}*");
    }

    [Fact]
    public void BillingContract_AddRate_NonOverlappingDatesForSameRole_AllowsBoth()
    {
        // Arrange
        var contract = CreateContract();
        var roleId = Guid.NewGuid();

        // First rate ends Mar 31; second starts Apr 1 — no overlap
        contract.AddRate(roleId, rate: 100m, unit: RateUnit.Hour,
            effectiveFrom: new DateOnly(2025, 1, 1),
            effectiveTo: new DateOnly(2025, 3, 31));

        // Act
        var act = () => contract.AddRate(roleId, rate: 120m, unit: RateUnit.Hour,
            effectiveFrom: new DateOnly(2025, 4, 1),
            effectiveTo: new DateOnly(2025, 6, 30));

        // Assert
        act.Should().NotThrow();
        contract.RateCards.Should().HaveCount(2);
    }

    [Fact]
    public void BillingContract_AddRate_DifferentRoles_AllowsOverlap()
    {
        // Arrange
        var contract = CreateContract();
        var role1 = Guid.NewGuid();
        var role2 = Guid.NewGuid();
        var from = new DateOnly(2025, 1, 1);
        var to = new DateOnly(2025, 12, 31);

        // Act — same date range but different roles should be fine
        contract.AddRate(role1, rate: 100m, unit: RateUnit.Hour, effectiveFrom: from, effectiveTo: to);
        var act = () => contract.AddRate(role2, rate: 150m, unit: RateUnit.Hour, effectiveFrom: from, effectiveTo: to);

        // Assert
        act.Should().NotThrow();
        contract.RateCards.Should().HaveCount(2);
    }

    [Fact]
    public void BillingContract_ResolveRate_ReturnsCorrectRateForDate()
    {
        // Arrange
        var contract = CreateContract();
        var roleId = Guid.NewGuid();

        contract.AddRate(roleId, rate: 100m, unit: RateUnit.Hour,
            effectiveFrom: new DateOnly(2025, 1, 1),
            effectiveTo: new DateOnly(2025, 6, 30));

        contract.AddRate(roleId, rate: 130m, unit: RateUnit.Hour,
            effectiveFrom: new DateOnly(2025, 7, 1),
            effectiveTo: null);

        // Act — query dates within each band
        var rateJan = contract.ResolveRate(roleId, new DateOnly(2025, 3, 15));
        var rateAug = contract.ResolveRate(roleId, new DateOnly(2025, 8, 1));

        // Assert
        rateJan.Should().Be(100m);
        rateAug.Should().Be(130m);
    }

    [Fact]
    public void BillingContract_ResolveRate_NoRateForDate_ReturnsNull()
    {
        // Arrange
        var contract = CreateContract();
        var roleId = Guid.NewGuid();

        // Rate only covers Jan–Jun 2025; query for Dec 2024 should fail
        contract.AddRate(roleId, rate: 100m, unit: RateUnit.Hour,
            effectiveFrom: new DateOnly(2025, 1, 1),
            effectiveTo: new DateOnly(2025, 6, 30));

        // Act — ResolveRate throws when no matching rate card exists
        var act = () => contract.ResolveRate(roleId, new DateOnly(2024, 12, 31));

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{roleId}*");
    }
}
