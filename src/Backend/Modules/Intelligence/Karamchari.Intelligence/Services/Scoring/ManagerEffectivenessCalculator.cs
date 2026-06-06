using Karamchari.Intelligence.Domain.Signals;

namespace Karamchari.Intelligence.Services.Scoring;

/// <summary>
/// Deterministic manager effectiveness calculator.
/// Pure static — no I/O, no dependencies.
/// Not computed for managers with fewer than 5 direct reports (statistically unreliable).
///
/// Formula weights:
///   Attendance 20% + Burnout 25% + Attrition 25% + LeaveHealth 10% + Stability 10% + OT 10%
/// </summary>
public static class ManagerEffectivenessCalculator
{
    public const int MinDirectReports = 5;

    /// <summary>
    /// Returns null if directReportCount &lt; MinDirectReports.
    /// </summary>
    public static ManagerEffectivenessResult? Calculate(ManagerEffectivenessInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.DirectReportCount < MinDirectReports)
            return null;

        // Attendance dimension: team attendance rate [0–1] → [0, 100]
        var attendance = Math.Clamp(input.TeamAttendanceRate * 100m, 0m, 100m);

        // Burnout dimension: high burnout fraction → low score
        var burnout = Math.Clamp((1m - input.HighBurnoutFraction) * 100m, 0m, 100m);

        // Attrition dimension: high attrition fraction → low score
        var attrition = Math.Clamp((1m - input.HighAttritionFraction) * 100m, 0m, 100m);

        // Leave health: low denial ratio = high score; 0 denials = 100
        var leaveHealth = Math.Clamp((1m - input.LeaveDenialRatio) * 100m, 0m, 100m);

        // Stability: fewer swaps = better; benchmark 3 swaps/member/month = 0 health
        var stability = Math.Clamp(100m - (input.AvgSwapsPerMemberPerMonth / 3m * 100m), 0m, 100m);

        // OT dependency: compare team OT vs org average; 2× org average → score 0
        var overtimeRatio = input.OrgAvgOvertimeHoursPerEmployee > 0m
            ? input.AvgOvertimeHoursPerMember / input.OrgAvgOvertimeHoursPerEmployee
            : (input.AvgOvertimeHoursPerMember > 0m ? 2m : 1m);
        var overtime = Math.Clamp(100m - ((overtimeRatio - 1m) * 100m), 0m, 100m);

        var overall = (attendance * 0.20m) + (burnout * 0.25m) + (attrition * 0.25m)
                    + (leaveHealth * 0.10m) + (stability * 0.10m) + (overtime * 0.10m);

        var explanation = BuildExplanation(input, attendance, burnout, attrition,
            leaveHealth, stability, overtime, Math.Round(overall, 1));

        return new ManagerEffectivenessResult(
            Math.Round(overall, 1),
            Math.Round(attendance, 1),
            Math.Round(burnout, 1),
            Math.Round(attrition, 1),
            Math.Round(leaveHealth, 1),
            Math.Round(stability, 1),
            Math.Round(overtime, 1),
            explanation);
    }

    private static ScoreExplanation BuildExplanation(
        ManagerEffectivenessInput input,
        decimal attendance, decimal burnout, decimal attrition,
        decimal leaveHealth, decimal stability, decimal overtime,
        decimal overall)
    {
        var dimensions = new[]
        {
            ("TeamAttendance", attendance, 0.20m),
            ("BurnoutRate", burnout, 0.25m),
            ("AttritionRate", attrition, 0.25m),
            ("LeaveHealth", leaveHealth, 0.10m),
            ("ShiftStability", stability, 0.10m),
            ("OvertimeDependency", overtime, 0.10m)
        };

        var contributors = dimensions
            .OrderByDescending(d => d.Item2)
            .Select(d => new ContributingFactor(d.Item1, d.Item3, EvidenceType.SystemCalculated,
                $"{d.Item1}: {d.Item2:F1}/100"))
            .ToList();

        var penalties = dimensions
            .Where(d => d.Item2 < 50m)
            .Select(d => new PenalizingFactor(d.Item1, d.Item3,
                $"{d.Item1} below 50 — team management risk"))
            .ToList();

        var summary = $"Manager effectiveness {overall:F0}/100 based on {input.DirectReportCount} reports. " +
                      $"High burnout: {input.HighBurnoutFraction:P0} of team. " +
                      $"High attrition risk: {input.HighAttritionFraction:P0} of team.";

        return ScoreExplanation.Compile(contributors, penalties, new List<MissingEvidence>(), summary);
    }
}
