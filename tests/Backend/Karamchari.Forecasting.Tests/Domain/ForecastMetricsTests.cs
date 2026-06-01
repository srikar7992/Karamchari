using FluentAssertions;
using Karamchari.Forecasting.Domain;
using Xunit;

namespace Karamchari.Forecasting.Tests.Domain;

public class ForecastMetricsTests
{
    [Fact]
    public void ForecastMetrics_Properties_StoreCorrectValues()
    {
        var tenantId = "tenant-abc";
        var date = new DateOnly(2026, 6, 1);

        var metrics = new ForecastMetrics
        {
            TenantId = tenantId,
            Date = date,
            ProjectedRevenue = 150_000m,
            ProjectedCash = 120_000m,
            OutstandingAmount = 75_000m,
            RiskAmount = 15_000m
        };

        metrics.TenantId.Should().Be(tenantId);
        metrics.Date.Should().Be(date);
        metrics.ProjectedRevenue.Should().Be(150_000m);
        metrics.ProjectedCash.Should().Be(120_000m);
        metrics.OutstandingAmount.Should().Be(75_000m);
        metrics.RiskAmount.Should().Be(15_000m);
    }

    [Fact]
    public void ForecastMetrics_Date_IsDateOnly()
    {
        var expectedDate = new DateOnly(2026, 3, 15);
        var metrics = new ForecastMetrics
        {
            Date = expectedDate
        };

        // DateOnly type is verified by the property type on ForecastMetrics
        metrics.Date.Year.Should().Be(2026);
        metrics.Date.Month.Should().Be(3);
        metrics.Date.Day.Should().Be(15);
        metrics.Date.Should().Be(expectedDate);
    }

    [Fact]
    public void ForecastMetrics_Id_IsGeneratedByDefault()
    {
        var metrics = new ForecastMetrics();

        metrics.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void ForecastMetrics_LastUpdatedAt_IsSetToUtcNowByDefault()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var metrics = new ForecastMetrics();

        metrics.LastUpdatedAt.Should().BeAfter(before);
    }

    [Fact]
    public void ForecastMetrics_ProjectedRevenue_CanBeUpdated()
    {
        var metrics = new ForecastMetrics { ProjectedRevenue = 100_000m };

        metrics.ProjectedRevenue = 200_000m;

        metrics.ProjectedRevenue.Should().Be(200_000m);
    }

    [Fact]
    public void ForecastMetrics_RiskAmount_DefaultsToZero()
    {
        var metrics = new ForecastMetrics();

        metrics.RiskAmount.Should().Be(0m);
    }
}
