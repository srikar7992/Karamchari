namespace Karamchari.TimeAttendance.Domain.Rostering.Scheduling;

/// <summary>
/// Scores eligible candidates for a shift using weighted formula.
/// Weights: Skill=45%, Availability=20%, Fairness=20%, Fatigue=15%.
/// Fairness is itself a composite: hours-load(50%) + night-shift(30%) + weekend(20%).
/// Distance excluded until location-proximity data exists — phantom metrics distort rankings.
/// </summary>
public static class CandidateScorer
{
    private const double SkillWeight = 0.45;
    private const double AvailabilityWeight = 0.20;
    private const double FairnessWeight = 0.20;
    private const double FatigueWeight = 0.15;

    // Fairness sub-weights (must sum to 1.0)
    private const double HoursFairnessWeight = 0.50;
    private const double NightFairnessWeight = 0.30;
    private const double WeekendFairnessWeight = 0.20;

    public static IReadOnlyList<CandidateScore> Score(
        IEnumerable<SchedulingCandidate> candidates,
        ShiftFillRequest request)
    {
        var eligible = candidates.Where(c => c.IsEligible).ToList();
        if (eligible.Count == 0) return [];

        return [.. eligible
            .Select(c => Score(c, request))
            .OrderByDescending(s => s.TotalScore)];
    }

    private static CandidateScore Score(SchedulingCandidate candidate, ShiftFillRequest request)
    {
        double skillScore = ComputeSkillScore(candidate, request);
        double availabilityScore = ComputeAvailabilityScore(candidate);
        (double fairnessScore, double hoursFairness, double nightFairness, double weekendFairness) = ComputeFairnessScore(candidate);
        double fatigueScore = ComputeFatigueScore(candidate, request.ShiftType);

        double total =
            skillScore * SkillWeight +
            availabilityScore * AvailabilityWeight +
            fairnessScore * FairnessWeight +
            fatigueScore * FatigueWeight;

        return new CandidateScore
        {
            EmployeeId = candidate.EmployeeId,
            SkillScore = skillScore,
            AvailabilityScore = availabilityScore,
            FairnessScore = fairnessScore,
            WeeklyHoursFairnessScore = hoursFairness,
            NightShiftFairnessScore = nightFairness,
            WeekendFairnessScore = weekendFairness,
            FatigueScore = fatigueScore,
            TotalScore = total,
        };
    }

    private static double ComputeSkillScore(SchedulingCandidate candidate, ShiftFillRequest request)
    {
        if (request.RequiredSkills.Count == 0) return 100.0;

        double score = 0;
        foreach (SkillRequirement req in request.RequiredSkills)
        {
            EmployeeSkill? match = candidate.Skills.FirstOrDefault(s =>
                s.SkillId == req.SkillId && s.IsValid(request.WorkDate));

            if (match == null) return 0.0; // missing required skill — hard block handled upstream

            score += Math.Min(100.0, (match.Level / (double)req.MinimumLevel) * 100.0);
        }

        return score / request.RequiredSkills.Count;
    }

    private static double ComputeAvailabilityScore(SchedulingCandidate candidate)
    {
        decimal max = candidate.OvertimePolicy.MaximumHours;
        if (max <= 0) return 100.0;
        decimal remaining = max - candidate.WeeklyHoursWorked;
        return Math.Clamp((double)(remaining / max) * 100.0, 0, 100);
    }

    private static (double composite, double hours, double nights, double weekends)
        ComputeFairnessScore(SchedulingCandidate candidate)
    {
        double hoursFairness = ComputeWeeklyHoursFairness(candidate);
        double nightFairness = ComputeNightShiftFairness(candidate);
        double weekendFairness = ComputeWeekendFairness(candidate);

        double composite =
            hoursFairness * HoursFairnessWeight +
            nightFairness * NightFairnessWeight +
            weekendFairness * WeekendFairnessWeight;

        return (composite, hoursFairness, nightFairness, weekendFairness);
    }

    private static double ComputeWeeklyHoursFairness(SchedulingCandidate candidate)
    {
        decimal max = candidate.OvertimePolicy.MaximumHours;
        if (max <= 0) return 100.0;
        // Lower hours worked relative to max = higher score (equalize load)
        return Math.Clamp((double)(1 - candidate.WeeklyHoursWorked / max) * 100.0, 0, 100);
    }

    private static double ComputeNightShiftFairness(SchedulingCandidate candidate)
    {
        return candidate.NightShiftsThisWeek switch
        {
            0 => 100.0,
            1 => 75.0,
            2 => 50.0,
            _ => 25.0,
        };
    }

    private static double ComputeWeekendFairness(SchedulingCandidate candidate)
    {
        return candidate.WeekendShiftsThisWeek switch
        {
            0 => 100.0,
            1 => 60.0,
            _ => 20.0,
        };
    }

    private static double ComputeFatigueScore(SchedulingCandidate candidate, ShiftType shiftType)
    {
        if (shiftType != ShiftType.Night) return 100.0;

        return candidate.ConsecutiveNightShifts switch
        {
            0 => 100.0,
            1 => 80.0,
            2 => 50.0,
            _ => 10.0,
        };
    }
}
