// -----------------------------------------------------------------------
// <copyright file="ClientPaymentProfileTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Forecasting.Domain;
using Xunit;

namespace Karamchari.Forecasting.Tests.Domain;

public class ClientPaymentProfileTests
{
    private static ClientPaymentProfile GoodPayingClient() => new()
    {
        TenantId = "tenant-1",
        ClientId = Guid.NewGuid(),
        AverageDaysToPay = 25,
        OnTimeRate = 0.95m,
        TotalInvoicesPaid = 50
    };

    private static ClientPaymentProfile LatePayingClient() => new()
    {
        TenantId = "tenant-1",
        ClientId = Guid.NewGuid(),
        AverageDaysToPay = 75, // > 60 days → late payer
        OnTimeRate = 0.40m,
        TotalInvoicesPaid = 20
    };

    [Fact]
    public void Profile_GetCollectionProbability_Within30Days_Returns95Percent()
    {
        var profile = GoodPayingClient();

        var prob = profile.GetCollectionProbability(15);

        prob.Should().Be(0.95m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_ExactlyAt30Days_Returns95Percent()
    {
        var profile = GoodPayingClient();

        var prob = profile.GetCollectionProbability(30);

        prob.Should().Be(0.95m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_31To60Days_Returns75Percent()
    {
        var profile = GoodPayingClient();

        var prob = profile.GetCollectionProbability(45);

        prob.Should().Be(0.75m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_ExactlyAt60Days_Returns75Percent()
    {
        var profile = GoodPayingClient();

        var prob = profile.GetCollectionProbability(60);

        prob.Should().Be(0.75m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_61To90Days_Returns50Percent()
    {
        var profile = GoodPayingClient();

        var prob = profile.GetCollectionProbability(75);

        prob.Should().Be(0.50m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_ExactlyAt90Days_Returns50Percent()
    {
        var profile = GoodPayingClient();

        var prob = profile.GetCollectionProbability(90);

        prob.Should().Be(0.50m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_Over90Days_Returns20Percent()
    {
        var profile = GoodPayingClient();

        var prob = profile.GetCollectionProbability(120);

        prob.Should().Be(0.20m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_LatePayingClient_PenaltyAppliedForShortDays()
    {
        // AverageDaysToPay=75 (> 60) and daysOutstanding=15 (< 60) → penalty applies
        var profile = LatePayingClient();

        var prob = profile.GetCollectionProbability(15);

        // base=0.95, penalty: 0.95 * 0.8 = 0.76
        prob.Should().Be(0.76m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_LatePayingClient_PenaltyAppliedAt30Days()
    {
        var profile = LatePayingClient();

        var prob = profile.GetCollectionProbability(30);

        // base=0.95, daysOutstanding=30 < 60, AverageDaysToPay=75 > 60 → penalty: 0.95 * 0.8 = 0.76
        prob.Should().Be(0.76m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_LatePayingClient_NoPenaltyForLongDays()
    {
        // daysOutstanding=70 (>= 60) → penalty guard does NOT apply (penalty condition: daysOutstanding < 60)
        var profile = LatePayingClient();

        var prob = profile.GetCollectionProbability(70);

        // base=0.50 (61-90 bucket), no penalty since daysOutstanding >= 60
        prob.Should().Be(0.50m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_LatePayingClient_NoPenaltyOver90Days()
    {
        // daysOutstanding=100 (> 90), penalty doesn't apply
        var profile = LatePayingClient();

        var prob = profile.GetCollectionProbability(100);

        prob.Should().Be(0.20m);
    }

    [Fact]
    public void Profile_GetCollectionProbability_GoodClient_AtExactly60Days_NoPenalty()
    {
        // Good paying client (AverageDaysToPay=25 ≤ 60) → no penalty regardless
        var profile = GoodPayingClient();

        var prob = profile.GetCollectionProbability(50);

        prob.Should().Be(0.75m);
    }
}
