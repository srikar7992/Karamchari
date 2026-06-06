using Karamchari.Intelligence.Domain.Signals;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Deterministic workforce health calculator (org/site level).
/// Pure static — no I/O, no dependencies.
///
/// Each dimension converts its raw metric to a [0, 100] health score
/// (higher = healthier). The overall score is the weighted sum.
///
/// Formula weights:
///   Attendance 25% + Burnout 25% + Staffing 20% + Leave 10% + Payroll 10% + Stability 10%
/// </summary>
public static class WorkforceHealthCalculator
{
    public static WorkforceHealthResult Calculate(WorkforceHealthInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        // Attendance: attendance rate [0–1] → [0, 100]
        var attendanceHealth = Math.Clamp(input.AttendanceRate * 100m, 0m, 100m);

        // Burnout: lower average burnout = healthier (invert)
        var burnoutHealth = Math.Clamp(100m - input.AverageBurnoutScore, 0m, 100m);

        // Staffing: coverage % [0–1] → [0, 100]
        var staffingHealth = Math.Clamp(input.AverageCoveragePct * 100m, 0m, 100m);

        // Leave: approval ratio [0–1] → [0, 100]
        var leaveHealth = Math.Clamp(input.LeaveApprovalRatio * 100m, 0m, 100m);

        // Payroll: OT fraction [0–1] — high OT spend → low health
        // >20% OT/payroll is a red flag → score: max(0, 100 - OTFraction*500)
        var payrollHealth = Math.Clamp(100m - (input.OvertimePayrollFraction * 500m), 0m, 100m);

        // Stability: more swaps = less stable. Benchmark: >3 swaps/emp/month → 0 health
        var stabilityHealth = Math.Clamp(100m - (input.AvgSwapsPerEmployeePerMonth / 3m * 100m), 0m, 100m);

        var overall = (attendanceHealth * 0.25m) + (burnoutHealth * 0.25m) + (staffingHealth * 0.20m)
                    + (leaveHealth * 0.10m) + (payrollHealth * 0.10m) + (stabilityHealth * 0.10m);

        var explanation = BuildExplanation(input, attendanceHealth, burnoutHealth,
            staffingHealth, leaveHealth, payrollHealth, stabilityHealth, Math.Round(overall, 1));

        return new WorkforceHealthResult(
            Math.Round(overall, 1),
            Math.Round(attendanceHealth, 1),
            Math.Round(burnoutHealth, 1),
            Math.Round(staffingHealth, 1),
            Math.Round(leaveHealth, 1),
            Math.Round(payrollHealth, 1),
            Math.Round(stabilityHealth, 1),
            explanation);
    }

    private static ScoreExplanation BuildExplanation(
        WorkforceHealthInput input,
        decimal attendance, decimal burnout, decimal staffing,
        decimal leave, decimal payroll, decimal stability,
        decimal overall)
    {
        var dimensions = new[]
        {
            ("Attendance", attendance, 0.25m),
            ("Burnout", burnout, 0.25m),
            ("Staffing", staffing, 0.20m),
            ("Leave", leave, 0.10m),
            ("Payroll", payroll, 0.10m),
            ("Stability", stability, 0.10m)
        };

        var contributors = dimensions
            .OrderByDescending(d => d.Item2)
            .Select(d => new ContributingFactor(d.Item1, d.Item3, EvidenceType.SystemCalculated,
                $"{d.Item1} health: {d.Item2:F1}/100"))
            .ToList();

        var penalties = dimensions
            .Where(d => d.Item2 < 50m)
            .Select(d => new PenalizingFactor(d.Item1, d.Item3, $"{d.Item1} below 50 — drag on overall health"))
            .ToList();

        var missing = new List<MissingEvidence>();
        if (input.EmployeeCount < 10)
            missing.Add(new MissingEvidence("SufficientHeadcount", "Fewer than 10 employees — site scores may not be representative"));

        var summary = $"Workforce health {overall:F0}/100 for site '{input.SiteCode}' " +
                      $"({input.EmployeeCount} employees). " +
                      $"Weakest dimension: {dimensions.OrderBy(d => d.Item2).First().Item1}.";

        return ScoreExplanation.Compile(contributors, penalties, missing, summary);
    }
}
