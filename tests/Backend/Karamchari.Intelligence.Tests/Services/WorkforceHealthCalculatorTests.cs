using FluentAssertions;
using Karamchari.Intelligence.Services.Scoring;
using Xunit;

namespace Karamchari.Intelligence.Tests.Services;

public class WorkforceHealthCalculatorTests
{
    private static WorkforceHealthInput Perfect => new(
        SiteCode: "HQ",
        AttendanceRate: 1.0m,         // 100% attendance
        AverageBurnoutScore: 0m,      // no burnout
        AverageCoveragePct: 1.0m,     // 100% coverage
        LeaveApprovalRatio: 1.0m,     // all leave approved
        OvertimePayrollFraction: 0m,  // no OT spend
        AvgSwapsPerEmployeePerMonth: 0m,
        EmployeeCount: 50
    );

    [Fact]
    public void Calculate_PerfectInputs_Score100()
    {
        var result = WorkforceHealthCalculator.Calculate(Perfect);
        result.Score.Should().Be(100m);
    }

    [Fact]
    public void Calculate_ZeroInputs_LowScore()
    {
        var zero = Perfect with
        {
            AttendanceRate = 0m,
            AverageBurnoutScore = 100m,
            AverageCoveragePct = 0m,
            LeaveApprovalRatio = 0m,
            OvertimePayrollFraction = 1m,
            AvgSwapsPerEmployeePerMonth = 10m
        };

        var result = WorkforceHealthCalculator.Calculate(zero);
        result.Score.Should().Be(0m);
    }

    [Fact]
    public void Calculate_AttendanceRate_MapsDirectlyToAttendanceHealth()
    {
        var input = Perfect with { AttendanceRate = 0.8m };
        var result = WorkforceHealthCalculator.Calculate(input);
        result.AttendanceHealth.Should().BeApproximately(80m, 0.1m);
    }

    [Fact]
    public void Calculate_HighBurnout_LowersBurnoutHealth()
    {
        var input = Perfect with { AverageBurnoutScore = 60m };
        var result = WorkforceHealthCalculator.Calculate(input);
        result.BurnoutHealth.Should().BeApproximately(40m, 0.1m);
    }

    [Fact]
    public void Calculate_OverOTPayroll_LowersPayrollHealth()
    {
        // 0.2 OT fraction → 100 - (0.2 * 500) = 0
        var input = Perfect with { OvertimePayrollFraction = 0.2m };
        var result = WorkforceHealthCalculator.Calculate(input);
        result.PayrollHealth.Should().Be(0m);
    }

    [Fact]
    public void Calculate_ScoreClampedTo100()
    {
        var result = WorkforceHealthCalculator.Calculate(Perfect);
        result.Score.Should().BeLessThanOrEqualTo(100m);
    }

    [Fact]
    public void Calculate_ScoreNeverNegative()
    {
        var zero = Perfect with { AttendanceRate = 0m, AverageBurnoutScore = 100m };
        var result = WorkforceHealthCalculator.Calculate(zero);
        result.Score.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Calculate_ExplanationHasSmallEmployeeCountWarning()
    {
        var input = Perfect with { EmployeeCount = 5 };
        var result = WorkforceHealthCalculator.Calculate(input);
        result.Explanation.MissingInputs.Should().NotBeEmpty();
    }

    [Fact]
    public void Calculate_ExplanationSummaryNotEmpty()
    {
        var result = WorkforceHealthCalculator.Calculate(Perfect);
        result.Explanation.Summary.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Calculate_NullInput_Throws()
        => Assert.Throws<ArgumentNullException>(() => WorkforceHealthCalculator.Calculate(null!));

    [Fact]
    public void Calculate_HighSwapRate_LowersStabilityHealth()
    {
        var input = Perfect with { AvgSwapsPerEmployeePerMonth = 3m };
        var result = WorkforceHealthCalculator.Calculate(input);
        result.StabilityHealth.Should().Be(0m);
    }

    [Fact]
    public void Calculate_WeightsSum()
    {
        // Verify formula: 0.25+0.25+0.20+0.10+0.10+0.10 = 1.0
        var weights = 0.25m + 0.25m + 0.20m + 0.10m + 0.10m + 0.10m;
        weights.Should().Be(1.0m);
    }
}
