using Karamchari.Compensation.Domain;

namespace Karamchari.Compensation.Tests.Domain;

public sealed class CompensationBandTests
{
    // ── helpers ─────────────────────────────────────────────────────────────

    private static CompensationBand ValidBand(
        decimal min = 50_000m,
        decimal mid = 75_000m,
        decimal max = 100_000m,
        DateOnly? from = null,
        DateOnly? to = null)
        => CompensationBand.Create(
            tenantId: "tenant-001",
            name: "SDE-II Band",
            jobFamily: "Engineering",
            careerLevelCode: "L4",
            bandType: CompensationBandType.BaseSalary,
            currencyCode: "INR",
            minimumAnnual: min,
            midpointAnnual: mid,
            maximumAnnual: max,
            effectiveFrom: from ?? new DateOnly(2026, 1, 1),
            effectiveTo: to);

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void CompensationBand_Create_ValidRange_Succeeds()
    {
        var band = ValidBand();

        band.Should().NotBeNull();
        band.Id.Should().NotBeEmpty();
        band.TenantId.Should().Be("tenant-001");
        band.Name.Should().Be("SDE-II Band");
        band.JobFamily.Should().Be("Engineering");
        band.CareerLevelCode.Should().Be("L4");
        band.BandType.Should().Be(CompensationBandType.BaseSalary);
        band.CurrencyCode.Should().Be("INR");
        band.MinimumAnnual.Should().Be(50_000m);
        band.MidpointAnnual.Should().Be(75_000m);
        band.MaximumAnnual.Should().Be(100_000m);
        band.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CompensationBand_Create_MinGreaterThanMax_ThrowsDomainException()
    {
        // min > mid violates the midpoint guard (midpoint <= min)
        Action act = () => ValidBand(min: 120_000m, mid: 75_000m, max: 100_000m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CompensationBand_Create_MidOutsideRange_ThrowsDomainException()
    {
        // mid == max means mid is not < max
        Action act = () => ValidBand(min: 50_000m, mid: 100_000m, max: 100_000m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void CompensationBand_Create_EffectiveToBeforeEffectiveFrom_ThrowsDomainException()
    {
        var from = new DateOnly(2026, 6, 1);
        var to = new DateOnly(2026, 1, 1); // before from

        Action act = () => ValidBand(from: from, to: to);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*EffectiveTo*");
    }

    // ── ComputeCompaRatio ────────────────────────────────────────────────────

    [Fact]
    public void CompensationBand_ComputeCompaRatio_ReturnsActualDividedByMidpointTimes100()
    {
        // midpoint = 75 000; actual = 60 000 → compa-ratio = 60000/75000*100 = 80.00
        var band = ValidBand(mid: 75_000m);

        var ratio = band.ComputeCompaRatio(60_000m);

        ratio.Should().Be(80.00m);
    }

    [Fact]
    public void CompensationBand_ComputeCompaRatio_AtMidpoint_Returns100()
    {
        var band = ValidBand(mid: 75_000m);

        var ratio = band.ComputeCompaRatio(75_000m);

        ratio.Should().Be(100.00m);
    }

    [Fact]
    public void CompensationBand_ComputeCompaRatio_AboveMidpoint_ReturnsAbove100()
    {
        // actual = 90 000; mid = 75 000 → 90000/75000*100 = 120.00
        var band = ValidBand(mid: 75_000m);

        var ratio = band.ComputeCompaRatio(90_000m);

        ratio.Should().Be(120.00m);
    }

    // ── Deactivate ───────────────────────────────────────────────────────────

    [Fact]
    public void CompensationBand_Deactivate_SetsIsActiveFalse()
    {
        var band = ValidBand();
        band.IsActive.Should().BeTrue(); // pre-condition

        band.Deactivate();

        band.IsActive.Should().BeFalse();
        band.EffectiveTo.Should().NotBeNull("Deactivate stamps EffectiveTo when null");
    }
}
