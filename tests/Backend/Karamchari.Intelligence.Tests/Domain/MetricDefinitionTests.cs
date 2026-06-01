using FluentAssertions;
using Karamchari.Intelligence.Domain.Metrics;
using Xunit;

namespace Karamchari.Intelligence.Tests.Domain;

public class MetricDefinitionTests
{
    private static CalculationDefinition DefaultCalc()
        => new("AVG(score)", "HR.Analytics", "Daily");

    private static MetricDefinition CreateMetric(string name = "FlightRiskScore")
        => MetricDefinition.Define("tenant-1", name, "HR.Analytics", DefaultCalc());

    [Fact]
    public void MetricDefinition_Define_SetsVersion1()
    {
        var metric = CreateMetric();

        metric.CurrentVersion.Should().Be("1.0");
    }

    [Fact]
    public void MetricDefinition_Define_SetsProperties()
    {
        var metric = CreateMetric("EngagementScore");

        metric.TenantId.Should().Be("tenant-1");
        metric.Name.Should().Be("EngagementScore");
        metric.Owner.Should().Be("HR.Analytics");
        metric.IsDeprecated.Should().BeFalse();
        metric.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void MetricDefinition_Evolve_IncrementsVersion()
    {
        var metric = CreateMetric();
        var newCalc = new CalculationDefinition("WEIGHTED_AVG(score, 0.7)", "HR.Analytics", "Hourly");

        metric.Evolve("2.0", newCalc);

        metric.CurrentVersion.Should().Be("2.0");
        metric.Calculation.Should().Be(newCalc);
        metric.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MetricDefinition_Evolve_SameVersion_ThrowsArgumentException()
    {
        var metric = CreateMetric();

        var act = () => metric.Evolve("1.0", DefaultCalc());

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void MetricDefinition_Evolve_WhenDeprecated_ThrowsInvalidOperationException()
    {
        var metric = CreateMetric();
        metric.Deprecate();

        var act = () => metric.Evolve("2.0", DefaultCalc());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MetricDefinition_Deprecate_SetsIsDeprecatedTrue()
    {
        var metric = CreateMetric();

        metric.Deprecate();

        metric.IsDeprecated.Should().BeTrue();
        metric.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MetricDefinition_Define_SetsCreatedAtUtc()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        var metric = CreateMetric();

        metric.CreatedAtUtc.Should().BeAfter(before);
    }
}
