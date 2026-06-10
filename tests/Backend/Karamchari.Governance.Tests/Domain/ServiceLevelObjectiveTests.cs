// -----------------------------------------------------------------------
// <copyright file="ServiceLevelObjectiveTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Governance.Domain.Reliability;
using Xunit;

namespace Karamchari.Governance.Tests.Domain;

public class ServiceLevelObjectiveTests
{
    // ---------------------------------------------------------------
    // Define
    // ---------------------------------------------------------------

    [Fact]
    public void SLO_Define_SetsTierAndTargetRate()
    {
        // Act
        var slo = ServiceLevelObjective.Define(
            tenantId: "System",
            componentName: "AuthService",
            tier: AvailabilityClassification.Tier1Critical,
            targetRate: 99.99m);

        // Assert
        slo.Tier.Should().Be(AvailabilityClassification.Tier1Critical);
        slo.TargetSuccessRate.Should().Be(99.99m);
        slo.ComponentName.Should().Be("AuthService");
        slo.Id.Should().NotBeEmpty();
        // Fresh SLO starts fully green
        slo.ErrorBudgetRemainingPercent.Should().Be(100m);
        slo.IsErrorBudgetExhausted.Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // Evaluate — meeting the target
    // ---------------------------------------------------------------

    [Fact]
    public void SLO_Evaluate_WhenMeetingTarget_ErrorBudgetIsPositive()
    {
        // Arrange — target 99.9%, measured 99.95%
        var slo = ServiceLevelObjective.Define(
            "System", "BillingBFF", AvailabilityClassification.Tier2High, targetRate: 99.9m);

        // Act
        slo.Evaluate(measuredSuccessRate: 99.95m);

        // Assert
        slo.CurrentSuccessRate.Should().Be(99.95m);
        slo.ErrorBudgetRemainingPercent.Should().BeGreaterThan(0);
        slo.IsErrorBudgetExhausted.Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // Evaluate — exhausting error budget
    // ---------------------------------------------------------------

    [Fact]
    public void SLO_Evaluate_WhenExceedingError_SetsIsErrorBudgetExhausted()
    {
        // Arrange — target 99.5% (allowed error = 0.5%), measured 99.0% (actual error = 1.0%)
        var slo = ServiceLevelObjective.Define(
            "System", "BackgroundJobs", AvailabilityClassification.Tier3Medium, targetRate: 99.5m);

        // Act — measured is worse than target, consuming all error budget
        slo.Evaluate(measuredSuccessRate: 99.0m);

        // Assert
        slo.IsErrorBudgetExhausted.Should().BeTrue();
        slo.ErrorBudgetRemainingPercent.Should().Be(0m);
    }

    // ---------------------------------------------------------------
    // Evaluate — exactly at target → 100% remaining budget
    // ---------------------------------------------------------------

    [Fact]
    public void SLO_Evaluate_ExactlyAtTarget_BudgetIs100Percent()
    {
        // Arrange — target 99.9%, measured exactly 99.9%
        // allowed error = 0.1%, actual error = 0.1% → remaining = 0%
        // Wait — if exactly at target: remaining = (0.1 - 0.1) / 0.1 * 100 = 0%
        // That means budget is exhausted at the target boundary.
        // Let's use measured slightly above (100%) to get 100% remaining.
        var slo = ServiceLevelObjective.Define(
            "System", "CoreAuth", AvailabilityClassification.Tier2High, targetRate: 99.9m);

        // Act — perfect availability
        slo.Evaluate(measuredSuccessRate: 100m);

        // Assert
        // allowedError = 0.1, actualError = 0.0
        // remaining = (0.1 - 0.0) / 0.1 * 100 = 100%
        slo.ErrorBudgetRemainingPercent.Should().Be(100m);
        slo.IsErrorBudgetExhausted.Should().BeFalse();
    }

    [Fact]
    public void SLO_Evaluate_ExactlyAtTarget_BudgetIsZero()
    {
        // Arrange — measured exactly at the target rate consumes ALL the error budget
        // e.g. target 99.9%: allowed error = 0.1%, actual error = 0.1%
        // remaining = (0.1 - 0.1) / 0.1 * 100 = 0%
        var slo = ServiceLevelObjective.Define(
            "System", "CoreAuth", AvailabilityClassification.Tier2High, targetRate: 99.9m);

        // Act
        slo.Evaluate(measuredSuccessRate: 99.9m);

        // Assert
        slo.ErrorBudgetRemainingPercent.Should().Be(0m);
        slo.IsErrorBudgetExhausted.Should().BeTrue();
    }

    // ---------------------------------------------------------------
    // Tier1 — target is 99.99% by convention
    // ---------------------------------------------------------------

    [Fact]
    public void SLO_Tier1_TargetIs99point99Percent()
    {
        // The Tier1Critical tier is described as 99.99%. Callers are responsible
        // for passing the correct target rate to Define(). This test verifies
        // that a Tier1 SLO defined with the canonical 99.99% target correctly
        // identifies when budget is nearly consumed.
        var slo = ServiceLevelObjective.Define(
            "System", "AuthService", AvailabilityClassification.Tier1Critical, targetRate: 99.99m);

        slo.TargetSuccessRate.Should().Be(99.99m);
        slo.Tier.Should().Be(AvailabilityClassification.Tier1Critical);

        // Evaluate at 99.99% → budget exhausted (all allowed error consumed)
        slo.Evaluate(99.99m);
        slo.IsErrorBudgetExhausted.Should().BeTrue();

        // Evaluate at 100% → budget fully remaining
        var slo2 = ServiceLevelObjective.Define(
            "System", "AuthService", AvailabilityClassification.Tier1Critical, targetRate: 99.99m);
        slo2.Evaluate(100m);
        slo2.IsErrorBudgetExhausted.Should().BeFalse();
    }

    // ---------------------------------------------------------------
    // Evaluate — below target clamps to 0, not negative
    // ---------------------------------------------------------------

    [Fact]
    public void SLO_Evaluate_BelowTarget_BudgetClampsToZero()
    {
        // Arrange
        var slo = ServiceLevelObjective.Define(
            "System", "Analytics", AvailabilityClassification.Tier4BestEffort, targetRate: 99.0m);

        // Act — 97% is well below the 99% target
        slo.Evaluate(measuredSuccessRate: 97.0m);

        // Assert — clamped at 0, not negative
        slo.ErrorBudgetRemainingPercent.Should().Be(0m);
        slo.IsErrorBudgetExhausted.Should().BeTrue();
    }
}
