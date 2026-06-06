using System.Text.Json;
using System.Text.Json.Serialization;

namespace Karamchari.Forecasting.Domain.Workforce;

public enum ForecastSource
{
    Historical = 1,
    PlannedWork = 2,
    Seasonal = 3,
    Manual = 4,
}

public enum ForecastConfidence
{
    High = 1,
    Medium = 2,
    Low = 3,
}

public enum ForecastHorizon
{
    Days7 = 7,
    Days14 = 14,
    Days30 = 30,
    Days90 = 90,
    Days180 = 180,
    Days365 = 365,
}

public enum WfpRiskLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
}

// ── Read Models ───────────────────────────────────────────────────────────────

/// <summary>
/// Maps an employee to their tenant, location, and skill set.
/// Updated by EmployeeOnboarded, EmployeeTerminated, SkillAssigned, SkillExpired events.
/// Used by the supply calculator to join leave records back to projections.
/// </summary>
public sealed class WorkforceEmployeeIndex
{
    private WorkforceEmployeeIndex() { TenantId = string.Empty; LocationCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTimeOffset LastUpdatedAt { get; private set; }

    [JsonInclude]
    public List<string> SkillCodes { get; private set; } = [];

    public static WorkforceEmployeeIndex Create(
        Guid employeeId,
        string tenantId,
        string locationCode = "DEFAULT")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new WorkforceEmployeeIndex
        {
            Id = employeeId,
            TenantId = tenantId,
            LocationCode = locationCode.Trim(),
            IsActive = true,
            LastUpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void Terminate()
    {
        IsActive = false;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AssignSkill(string skillCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(skillCode);
        if (!SkillCodes.Contains(skillCode))
            SkillCodes.Add(skillCode);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void RevokeSkill(string skillCode)
    {
        SkillCodes.Remove(skillCode);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateLocation(string locationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(locationCode);
        LocationCode = locationCode.Trim();
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Optional date of birth. Used to derive ExpectedRetirementDate when skills are assigned.</summary>
    public DateOnly? DateOfBirth { get; private set; }

    /// <summary>Optional contract end date for fixed-term employees. Used to create WfpContractExpiry per skill.</summary>
    public DateOnly? ContractEndDate { get; private set; }

    public void SetDateOfBirth(DateOnly dob) { DateOfBirth = dob; LastUpdatedAt = DateTimeOffset.UtcNow; }
    public void SetContractEndDate(DateOnly endDate) { ContractEndDate = endDate; LastUpdatedAt = DateTimeOffset.UtcNow; }
}

/// <summary>
/// Tracks future skill certification expiry dates per employee.
/// Created on SkillAssigned (when ExpiryDate is non-null), revoked on SkillExpired or renewal.
/// Supply forecast uses this to reduce available headcount before expiry date.
/// </summary>
public sealed class WfpSkillExpiry
{
    private WfpSkillExpiry() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public DateOnly ExpiryDate { get; private set; }
    public bool IsRevoked { get; private set; }

    public static WfpSkillExpiry Create(
        Guid employeeId,
        string tenantId,
        string locationCode,
        string skillCode,
        DateOnly expiryDate)
    {
        return new WfpSkillExpiry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            ExpiryDate = expiryDate,
        };
    }

    public void Revoke() => IsRevoked = true;
    public void RenewExpiry(DateOnly newExpiry) => ExpiryDate = newExpiry;
}

/// <summary>
/// Daily attendance snapshot per (tenant, location, skill).
/// Populated by TimesheetApproved events. Feeds the historical demand model
/// and day-of-week absence rate calculations.
/// ActualHeadcount = employees who submitted hours. ExpectedHeadcount = TotalEmployees at snapshot time.
/// </summary>
public sealed class WfpAttendanceSnapshot
{
    private WfpAttendanceSnapshot() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public DateOnly Date { get; private set; }
    public int ActualHeadcount { get; private set; }
    public int ExpectedHeadcount { get; private set; }

    public static WfpAttendanceSnapshot Create(
        string tenantId, string locationCode, string skillCode, DateOnly date, int expectedHeadcount)
    {
        return new WfpAttendanceSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            Date = date,
            ExpectedHeadcount = expectedHeadcount,
            ActualHeadcount = 0,
        };
    }

    public void IncrementActual() => ActualHeadcount++;
    public void SetExpected(int count) => ExpectedHeadcount = count;
}

/// <summary>
/// Monthly termination count per (tenant, location, skill).
/// Accumulates on EmployeeTerminated events to compute rolling quarterly attrition rate.
/// </summary>
public sealed class WfpAttritionSnapshot
{
    private WfpAttritionSnapshot() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public int TerminationCount { get; private set; }

    public static WfpAttritionSnapshot Create(string tenantId, string locationCode, string skillCode, int year, int month)
    {
        return new WfpAttritionSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            Year = year,
            Month = month,
        };
    }

    public void IncrementTerminations() => TerminationCount++;
}

/// <summary>
/// Per-(tenant, location, skill) aggregate for supply-side state.
/// TotalEmployees, absenteeism, reliability, attrition rate, and day-of-week patterns
/// drive SupplyForecast computation.
/// </summary>
public sealed class WorkforcePlanningProjection
{
    private WorkforcePlanningProjection() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public int TotalEmployees { get; private set; }

    /// <summary>Rolling 90-day average absenteeism as a percentage (e.g. 3.0 = 3%). Fallback when day-of-week rates absent.</summary>
    public decimal HistoricalAbsenteeismRate { get; private set; }

    /// <summary>Weighted average attendance reliability (0.0–1.0). Applied to expected hours.</summary>
    public decimal AttendanceReliabilityScore { get; private set; } = 0.95m;

    /// <summary>Rolling quarterly attrition rate as a percentage (e.g. 4.0 = 4% per quarter).</summary>
    public decimal HistoricalAttritionRatePercent { get; private set; }

    /// <summary>Number of attendance snapshots recorded. Drives demand forecast confidence scoring.</summary>
    public int DataPointsCount { get; private set; }

    /// <summary>
    /// JSON: Dictionary&lt;string, decimal&gt; where key = "{dow}_{month}" (e.g. "1_3" = Monday in March)
    /// or "{dow}" (day-only fallback). Value = absence rate %. Empty = use HistoricalAbsenteeismRate.
    /// </summary>
    public string DayOfWeekAbsenceRatesJson { get; private set; } = "{}";

    public DateTimeOffset LastUpdatedAt { get; private set; }

    public static WorkforcePlanningProjection Create(string tenantId, string locationCode, string skillCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(skillCode);

        return new WorkforcePlanningProjection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            LastUpdatedAt = DateTimeOffset.UtcNow,
        };
    }

    public void IncrementHeadcount() { TotalEmployees++; LastUpdatedAt = DateTimeOffset.UtcNow; }
    public void DecrementHeadcount() { TotalEmployees = Math.Max(0, TotalEmployees - 1); LastUpdatedAt = DateTimeOffset.UtcNow; }

    public void UpdateAbsenteeismRate(decimal ratePercent)
    {
        HistoricalAbsenteeismRate = Math.Max(0m, ratePercent);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateReliabilityScore(decimal score)
    {
        AttendanceReliabilityScore = Math.Clamp(score, 0m, 1m);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateAttritionRate(decimal ratePercent)
    {
        HistoricalAttritionRatePercent = Math.Max(0m, ratePercent);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void IncrementDataPoints()
    {
        DataPointsCount++;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateAbsenceRates(Dictionary<string, decimal> rates)
    {
        DayOfWeekAbsenceRatesJson = JsonSerializer.Serialize(rates);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Looks up absence rate for a specific forecast date.
    /// Priority: composite key "{dow}_{month}" → day-only key "{dow}" → HistoricalAbsenteeismRate fallback.
    /// </summary>
    public decimal GetAbsenceRate(DateOnly forecastDate)
    {
        if (string.IsNullOrEmpty(DayOfWeekAbsenceRatesJson) || DayOfWeekAbsenceRatesJson == "{}")
            return HistoricalAbsenteeismRate;
        try
        {
            var rates = JsonSerializer.Deserialize<Dictionary<string, decimal>>(DayOfWeekAbsenceRatesJson);
            if (rates == null) return HistoricalAbsenteeismRate;
            var compositeKey = $"{(int)forecastDate.DayOfWeek}_{forecastDate.Month}";
            if (rates.TryGetValue(compositeKey, out var composite)) return composite;
            var dayKey = $"{(int)forecastDate.DayOfWeek}";
            if (rates.TryGetValue(dayKey, out var dayOnly)) return dayOnly;
            return HistoricalAbsenteeismRate;
        }
        catch { return HistoricalAbsenteeismRate; }
    }

    [Obsolete("Use UpdateAbsenceRates(Dictionary<string, decimal>) for composite day+month keys.")]
    public void UpdateDayOfWeekAbsenceRates(Dictionary<int, decimal> rates)
    {
        var stringRates = rates.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
        DayOfWeekAbsenceRatesJson = JsonSerializer.Serialize(stringRates);
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    [Obsolete("Use GetAbsenceRate(DateOnly) for composite day+month lookup.")]
    public Dictionary<int, decimal> GetDayOfWeekAbsenceRates()
    {
        if (string.IsNullOrEmpty(DayOfWeekAbsenceRatesJson) || DayOfWeekAbsenceRatesJson == "{}")
            return [];
        try
        {
            var raw = JsonSerializer.Deserialize<Dictionary<string, decimal>>(DayOfWeekAbsenceRatesJson);
            if (raw == null) return [];
            var result = new Dictionary<int, decimal>();
            foreach (var (k, v) in raw)
                if (int.TryParse(k.Split('_')[0], out var dow))
                    result.TryAdd(dow, v);
            return result;
        }
        catch { return []; }
    }
}

/// <summary>
/// Individual approved leave record for future-date availability calculations.
/// Cancelled when LeaveCancelled event arrives.
/// </summary>
public sealed class WfpApprovedLeaveRecord
{
    private WfpApprovedLeaveRecord() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public Guid RequestId { get; private set; }
    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsCancelled { get; private set; }

    public static WfpApprovedLeaveRecord Create(
        Guid requestId,
        string tenantId,
        Guid employeeId,
        string locationCode,
        string skillCode,
        DateOnly startDate,
        DateOnly endDate)
    {
        return new WfpApprovedLeaveRecord
        {
            Id = Guid.NewGuid(),
            RequestId = requestId,
            TenantId = tenantId,
            EmployeeId = employeeId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            StartDate = startDate,
            EndDate = endDate,
        };
    }

    public void Cancel() => IsCancelled = true;
    public bool CoversDate(DateOnly date) => !IsCancelled && date >= StartDate && date <= EndDate;
}

// ── Forecast Aggregates ───────────────────────────────────────────────────────

/// <summary>
/// Workload-based demand snapshot per (location, skill, date, horizon).
/// Generated by DemandForecastGenerator: Historical (rolling average), Seasonal (multiplied), or Manual (manager override).
/// Confidence reflects data quality: High = 90+ data points, Medium = 30-89, Low = under 30.
/// </summary>
public sealed class DemandForecast
{
    private DemandForecast() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public DateOnly ForecastDate { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public decimal RequiredHours { get; private set; }
    public int RequiredHeadcount { get; private set; }
    public ForecastSource Source { get; private set; }
    public ForecastConfidence Confidence { get; private set; }
    public ForecastHorizon Horizon { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    public static DemandForecast Create(
        string tenantId,
        DateOnly forecastDate,
        string locationCode,
        string skillCode,
        int requiredHeadcount,
        ForecastHorizon horizon,
        ForecastSource source = ForecastSource.Historical,
        ForecastConfidence confidence = ForecastConfidence.Medium,
        decimal hoursPerHead = 8m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(requiredHeadcount);

        return new DemandForecast
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ForecastDate = forecastDate,
            LocationCode = locationCode,
            SkillCode = skillCode,
            RequiredHeadcount = requiredHeadcount,
            RequiredHours = requiredHeadcount * hoursPerHead,
            Source = source,
            Confidence = confidence,
            Horizon = horizon,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// Employee-availability prediction for a future date.
/// Accounts for: approved leave, skill expiry, expected attrition (horizon-scaled), unplanned absence.
/// </summary>
public sealed class SupplyForecast
{
    private SupplyForecast() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public DateOnly ForecastDate { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public int TotalHeadcount { get; private set; }
    public int ApprovedLeaveCount { get; private set; }
    /// <summary>Employees whose relevant skill expires before ForecastDate.</summary>
    public int SkillExpiryCount { get; private set; }
    /// <summary>Employees predicted to leave during the planning horizon (attrition model).</summary>
    public int ExpectedAttrition { get; private set; }
    /// <summary>Unplanned absences: sick leave, no-shows (from historical day-of-week rates).</summary>
    public int ExpectedAbsent { get; private set; }
    /// <summary>Future hires (offer accepted, not yet onboarded) expected to start on or before ForecastDate.</summary>
    public int FutureHireAddition { get; private set; }
    /// <summary>Fixed-term employees whose contract ends before ForecastDate.</summary>
    public int ContractExpiryDeduction { get; private set; }
    /// <summary>Employees expected to retire before ForecastDate.</summary>
    public int RetirementDeduction { get; private set; }
    public int ExpectedHeadcount { get; private set; }
    public decimal ExpectedHours { get; private set; }
    public decimal ReliabilityScore { get; private set; }
    public ForecastHorizon Horizon { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    public static SupplyForecast Create(
        string tenantId,
        DateOnly forecastDate,
        string locationCode,
        string skillCode,
        int totalHeadcount,
        int approvedLeaveCount,
        int skillExpiryCount,
        int expectedAttrition,
        int expectedAbsent,
        ForecastHorizon horizon,
        decimal reliabilityScore,
        int futureHireAddition = 0,
        int contractExpiryDeduction = 0,
        int retirementDeduction = 0,
        decimal hoursPerHead = 8m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var adjustedTotal = Math.Max(0,
            totalHeadcount
            + futureHireAddition
            - skillExpiryCount
            - expectedAttrition
            - contractExpiryDeduction
            - retirementDeduction);
        var expectedHeadcount = Math.Max(0, adjustedTotal - approvedLeaveCount - expectedAbsent);

        return new SupplyForecast
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ForecastDate = forecastDate,
            LocationCode = locationCode,
            SkillCode = skillCode,
            TotalHeadcount = totalHeadcount,
            ApprovedLeaveCount = approvedLeaveCount,
            SkillExpiryCount = skillExpiryCount,
            ExpectedAttrition = expectedAttrition,
            ExpectedAbsent = expectedAbsent,
            FutureHireAddition = futureHireAddition,
            ContractExpiryDeduction = contractExpiryDeduction,
            RetirementDeduction = retirementDeduction,
            ExpectedHeadcount = expectedHeadcount,
            ExpectedHours = Math.Round(expectedHeadcount * hoursPerHead * reliabilityScore, 1),
            ReliabilityScore = reliabilityScore,
            Horizon = horizon,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// Intelligence layer: Gap = Supply - Demand. Negative = shortfall.
/// Gap &gt;= 0: Low. -1 to -5: Medium. -6 to -15: High. &lt; -15: Critical.
/// </summary>
public sealed class CoverageRisk
{
    private CoverageRisk() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public DateOnly RiskDate { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public int DemandHeadcount { get; private set; }
    public int SupplyHeadcount { get; private set; }
    /// <summary>Supply - Demand. Negative = shortfall. Positive = surplus.</summary>
    public int Gap { get; private set; }
    public WfpRiskLevel RiskLevel { get; private set; }
    public ForecastHorizon Horizon { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    public static CoverageRisk Compute(
        string tenantId,
        DateOnly riskDate,
        string locationCode,
        string skillCode,
        int demandHeadcount,
        int supplyHeadcount,
        ForecastHorizon horizon)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var gap = supplyHeadcount - demandHeadcount;
        var riskLevel = gap switch
        {
            >= 0 => WfpRiskLevel.Low,
            >= -5 => WfpRiskLevel.Medium,
            >= -15 => WfpRiskLevel.High,
            _ => WfpRiskLevel.Critical,
        };

        return new CoverageRisk
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RiskDate = riskDate,
            LocationCode = locationCode,
            SkillCode = skillCode,
            DemandHeadcount = demandHeadcount,
            SupplyHeadcount = supplyHeadcount,
            Gap = gap,
            RiskLevel = riskLevel,
            Horizon = horizon,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// Converts coverage risk into a hiring recommendation.
/// OvertimeCapacity = floor(availableHeadcount * OvertimeCapacityFactor).
/// NetHiringNeeded = max(0, GapHeadcount - OvertimeCapacity - CrossSkillCapacity).
/// </summary>
public sealed class HiringGap
{
    private HiringGap() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public DateOnly PlanningPeriod { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public int RequiredHeadcount { get; private set; }
    public int AvailableHeadcount { get; private set; }
    /// <summary>RequiredHeadcount - AvailableHeadcount (positive = shortage).</summary>
    public int GapHeadcount { get; private set; }
    /// <summary>Existing workforce overtime capacity (default 10% of available headcount).</summary>
    public int OvertimeCapacityHeadcount { get; private set; }
    /// <summary>Cross-trained employees who could substitute (requires skill substitution config; 0 until configured).</summary>
    public int CrossSkillCapacityHeadcount { get; private set; }
    /// <summary>Headcount that must be hired externally after internal capacity is exhausted.</summary>
    public int NetHiringNeeded { get; private set; }
    public ForecastHorizon Horizon { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    public static HiringGap? ComputeIfNeeded(
        string tenantId,
        DateOnly planningPeriod,
        string locationCode,
        string skillCode,
        int requiredHeadcount,
        int availableHeadcount,
        ForecastHorizon horizon,
        int crossSkillCapacityHeadcount = 0,
        decimal overtimeCapacityFactor = 0.10m)
    {
        var gap = requiredHeadcount - availableHeadcount;
        if (gap <= 0) return null;

        var overtimeCapacity = (int)Math.Floor(availableHeadcount * overtimeCapacityFactor);
        var netHiringNeeded = Math.Max(0, gap - overtimeCapacity - crossSkillCapacityHeadcount);

        return new HiringGap
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanningPeriod = planningPeriod,
            LocationCode = locationCode,
            SkillCode = skillCode,
            RequiredHeadcount = requiredHeadcount,
            AvailableHeadcount = availableHeadcount,
            GapHeadcount = gap,
            OvertimeCapacityHeadcount = overtimeCapacity,
            CrossSkillCapacityHeadcount = crossSkillCapacityHeadcount,
            NetHiringNeeded = netHiringNeeded,
            Horizon = horizon,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }
}

// ── Scenario Planning ─────────────────────────────────────────────────────────

/// <summary>
/// Named what-if scenario. Stored per tenant. ScenarioEngine runs it against all projections
/// and writes WfpScenarioResult rows.
/// </summary>
public sealed class WfpScenarioForecast
{
    private WfpScenarioForecast() { TenantId = string.Empty; Name = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public string Name { get; private set; }
    /// <summary>Multiplier applied to demand (e.g. 1.20 = +20% demand).</summary>
    public decimal DemandMultiplier { get; private set; } = 1.0m;
    /// <summary>Percentage points added to absenteeism rate (e.g. 5.0 = +5pp above baseline).</summary>
    public decimal AbsenteeismDeltaPercent { get; private set; }
    /// <summary>Percentage points added to quarterly attrition rate.</summary>
    public decimal AttritionDeltaPercent { get; private set; }
    /// <summary>Additional headcount hired before forecast date (positive = more supply).</summary>
    public int AdditionalHireCount { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static WfpScenarioForecast Create(
        string tenantId,
        string name,
        decimal demandMultiplier = 1.0m,
        decimal absenteeismDeltaPercent = 0m,
        decimal attritionDeltaPercent = 0m,
        int additionalHireCount = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new WfpScenarioForecast
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name,
            DemandMultiplier = Math.Max(0.01m, demandMultiplier),
            AbsenteeismDeltaPercent = absenteeismDeltaPercent,
            AttritionDeltaPercent = attritionDeltaPercent,
            AdditionalHireCount = additionalHireCount,
            CreatedAt = DateTimeOffset.UtcNow,
            IsActive = true,
        };
    }

    public void Deactivate() => IsActive = false;
}

// ── Phase 5.1 Hardening Models ────────────────────────────────────────────────

/// <summary>
/// Cross-skill substitution rule: employees with SubstituteSkillCode can partially fill
/// gaps for SkillCode at CapacityFraction efficiency.
/// Used by HiringGap to reduce external hiring requirement.
/// </summary>
/// <remarks>
/// CYCLE INVARIANT — Traversal is intentionally single-hop (A → B only, never A → B → C).
/// Direct cycles (A → A) are blocked by Create(). Indirect cycles (A → B → A) cannot
/// produce infinite loops in current code because substitution lookup never recurses.
///
/// If a future developer adds recursive traversal, they MUST add a cycle-detection pass
/// (e.g., visited-set DFS or topological sort across all active substitutions per tenant)
/// before enabling multi-hop. This invariant is the extension contract.
/// </remarks>
public sealed class WfpSkillSubstitution
{
    private WfpSkillSubstitution() { TenantId = string.Empty; SkillCode = string.Empty; SubstituteSkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    /// <summary>The skill with a shortage that needs filling.</summary>
    public string SkillCode { get; private set; }
    /// <summary>The skill whose holders can substitute (e.g. FORKLIFT_ADVANCED for FORKLIFT_BASIC).</summary>
    public string SubstituteSkillCode { get; private set; }
    /// <summary>Fraction of each substitute employee's capacity that applies (0.0–1.0). E.g. 0.80 = 80%.</summary>
    public decimal CapacityFraction { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static WfpSkillSubstitution Create(
        string tenantId, string skillCode, string substituteSkillCode, decimal capacityFraction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(skillCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(substituteSkillCode);
        if (string.Equals(skillCode.Trim(), substituteSkillCode.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("A skill cannot substitute itself — direct cycles are not permitted.", nameof(substituteSkillCode));

        return new WfpSkillSubstitution
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            SkillCode = skillCode,
            SubstituteSkillCode = substituteSkillCode,
            CapacityFraction = Math.Clamp(capacityFraction, 0m, 1m),
        };
    }

    public void Deactivate() => IsActive = false;
}

/// <summary>
/// Policy-based overtime capacity per (tenant, location).
/// Overtime factor = (MaxLegalWeeklyHours - StandardWeeklyHours) / StandardWeeklyHours.
/// Replaces the hardcoded 10% assumption in HiringGap.
/// </summary>
public sealed class WfpOvertimePolicy
{
    private WfpOvertimePolicy() { TenantId = string.Empty; LocationCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    /// <summary>Normal contractual hours per week (e.g. 40.0).</summary>
    public decimal StandardWeeklyHours { get; private set; }
    /// <summary>Maximum legal/union-permitted hours per week (e.g. 48.0).</summary>
    public decimal MaxLegalWeeklyHours { get; private set; }

    /// <summary>Fraction of headcount that overtime absorbs (e.g. 48h vs 40h standard = 0.20 = 20%).</summary>
    public decimal OvertimeFactor =>
        StandardWeeklyHours > 0
            ? (MaxLegalWeeklyHours - StandardWeeklyHours) / StandardWeeklyHours
            : 0.10m;

    public static WfpOvertimePolicy Create(
        string tenantId, string locationCode, decimal standardWeeklyHours, decimal maxLegalWeeklyHours)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(locationCode);

        return new WfpOvertimePolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationCode = locationCode,
            StandardWeeklyHours = Math.Max(1m, standardWeeklyHours),
            MaxLegalWeeklyHours = Math.Max(standardWeeklyHours, maxLegalWeeklyHours),
        };
    }

    public void UpdateHours(decimal standard, decimal maxLegal)
    {
        StandardWeeklyHours = Math.Max(1m, standard);
        MaxLegalWeeklyHours = Math.Max(StandardWeeklyHours, maxLegal);
    }
}

/// <summary>
/// Records predicted vs actual headcount per (tenant, location, skill, forecastDate, horizon).
/// Populated nightly after recompute by ForecastAccuracyCalculator.
/// Tracks MAPE to determine whether forecasts are improving or degrading over time.
/// </summary>
public sealed class WfpForecastAccuracy
{
    private WfpForecastAccuracy() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public DateOnly ForecastDate { get; private set; }
    public ForecastHorizon Horizon { get; private set; }
    public int PredictedHeadcount { get; private set; }
    public int ActualHeadcount { get; private set; }
    public int AbsoluteError { get; private set; }
    /// <summary>Mean Absolute Percentage Error for this single observation (0–100).</summary>
    public decimal AbsolutePercentError { get; private set; }
    /// <summary>Predicted - Actual. Positive = over-forecast (staffed too many). Negative = under-forecast (shortfall).</summary>
    public int SignedError { get; private set; }
    /// <summary>SignedError^2 for RMSE computation: sqrt(avg(SquaredError)) over a window gives RMSE.</summary>
    public decimal SquaredError { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    public static WfpForecastAccuracy Record(
        string tenantId,
        string locationCode,
        string skillCode,
        DateOnly forecastDate,
        ForecastHorizon horizon,
        int predictedHeadcount,
        int actualHeadcount)
    {
        var signedErr = predictedHeadcount - actualHeadcount;
        var absErr = Math.Abs(signedErr);
        var mape = actualHeadcount > 0
            ? Math.Round((decimal)absErr / actualHeadcount * 100m, 2)
            : (predictedHeadcount > 0 ? 100m : 0m);

        return new WfpForecastAccuracy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            ForecastDate = forecastDate,
            Horizon = horizon,
            PredictedHeadcount = predictedHeadcount,
            ActualHeadcount = actualHeadcount,
            AbsoluteError = absErr,
            AbsolutePercentError = mape,
            SignedError = signedErr,
            SquaredError = (decimal)signedErr * signedErr,
            RecordedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// Debounce buffer for incremental recomputes.
/// Consumer events enqueue dirty (tenant, location, skill) keys here instead of triggering
/// immediate recompute. RecomputeQueueDrainer flushes every 30 seconds, coalescing N events
/// for the same projection into a single recompute call.
/// </summary>
public sealed class WfpRecomputeEntry
{
    private WfpRecomputeEntry() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public DateTimeOffset QueuedAt { get; private set; }

    public static WfpRecomputeEntry Enqueue(string tenantId, string locationCode, string skillCode)
    {
        return new WfpRecomputeEntry
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            QueuedAt = DateTimeOffset.UtcNow,
        };
    }
}

/// <summary>
/// Result of running a WfpScenarioForecast against one (location, skill, horizon) combination.
/// </summary>
public sealed class WfpScenarioResult
{
    private WfpScenarioResult() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public Guid ScenarioId { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public ForecastHorizon Horizon { get; private set; }
    public int ScenarioDemandHeadcount { get; private set; }
    public int ScenarioSupplyHeadcount { get; private set; }
    public int ScenarioGap { get; private set; }
    public WfpRiskLevel ScenarioRiskLevel { get; private set; }
    public int ScenarioNetHiringNeeded { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    public static WfpScenarioResult Compute(
        Guid scenarioId,
        string tenantId,
        string locationCode,
        string skillCode,
        ForecastHorizon horizon,
        int scenarioDemand,
        int scenarioSupply)
    {
        var gap = scenarioSupply - scenarioDemand;
        var risk = gap switch
        {
            >= 0 => WfpRiskLevel.Low,
            >= -5 => WfpRiskLevel.Medium,
            >= -15 => WfpRiskLevel.High,
            _ => WfpRiskLevel.Critical,
        };
        var netHiring = gap < 0 ? Math.Abs(gap) : 0;

        return new WfpScenarioResult
        {
            Id = Guid.NewGuid(),
            ScenarioId = scenarioId,
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            Horizon = horizon,
            ScenarioDemandHeadcount = scenarioDemand,
            ScenarioSupplyHeadcount = scenarioSupply,
            ScenarioGap = gap,
            ScenarioRiskLevel = risk,
            ScenarioNetHiringNeeded = netHiring,
            GeneratedAt = DateTimeOffset.UtcNow,
        };
    }
}

// ── Phase 5.x Enterprise Enhancements ────────────────────────────────────────

/// <summary>
/// Tracks a future employee whose offer is accepted but who has not yet onboarded.
/// Inflates supply forecasts for dates on or after ExpectedStartDate, weighted by JoiningProbability.
/// JoiningProbability reflects offer drop-off risk (e.g. 0.75 for graduate hires, 0.98 for internal transfers).
/// Supply contribution = SUM(JoiningProbability) rounded to nearest integer, not a raw count.
/// Marked IsOnboarded when EmployeeOnboardedIntegrationEvent arrives for the same tenant+location+skill window.
/// </summary>
public sealed class WfpFutureHire
{
    private WfpFutureHire() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public Guid OfferId { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public DateOnly ExpectedStartDate { get; private set; }
    /// <summary>
    /// Probability that this accepted offer results in the candidate actually joining (0.0–1.0).
    /// Default 1.0 (certain to join). Set lower for graduate/external hires with high drop-off rates.
    /// </summary>
    public decimal JoiningProbability { get; private set; } = 1.0m;
    /// <summary>Set when an EmployeeOnboarded event is received for the matching tenant+location+skill window.</summary>
    public bool IsOnboarded { get; private set; }

    public static WfpFutureHire Create(
        Guid offerId, string tenantId, string locationCode, string skillCode, DateOnly expectedStartDate,
        decimal joiningProbability = 1.0m)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new WfpFutureHire
        {
            Id = Guid.NewGuid(),
            OfferId = offerId,
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            ExpectedStartDate = expectedStartDate,
            JoiningProbability = Math.Clamp(joiningProbability, 0m, 1m),
        };
    }

    public void MarkOnboarded() => IsOnboarded = true;
}

/// <summary>
/// Tracks fixed-term contract end dates per (employee, location, skill).
/// Supply forecast counts employees whose ContractEndDate falls before ForecastDate as no longer available.
/// Marked IsTerminated when EmployeeTerminated fires (the actual termination is already tracked via headcount decrement).
/// </summary>
public sealed class WfpContractExpiry
{
    private WfpContractExpiry() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public DateOnly ContractEndDate { get; private set; }
    public bool IsTerminated { get; private set; }

    public static WfpContractExpiry Create(
        Guid employeeId, string tenantId, string locationCode, string skillCode, DateOnly contractEndDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new WfpContractExpiry
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            ContractEndDate = contractEndDate,
        };
    }

    public void MarkTerminated() => IsTerminated = true;
}

/// <summary>
/// Tracks expected retirement date per (employee, location, skill).
/// Derived from DateOfBirth + configurable retirement age (default 60 years).
/// Supply forecast counts employees expected to retire before ForecastDate as no longer available.
/// Marked IsRetired when EmployeeTerminated fires.
/// </summary>
public sealed class WfpRetirementRisk
{
    private WfpRetirementRisk() { TenantId = string.Empty; LocationCode = string.Empty; SkillCode = string.Empty; }

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string TenantId { get; private set; }
    public string LocationCode { get; private set; }
    public string SkillCode { get; private set; }
    public DateOnly ExpectedRetirementDate { get; private set; }
    public bool IsRetired { get; private set; }

    public static WfpRetirementRisk Create(
        Guid employeeId, string tenantId, string locationCode, string skillCode, DateOnly expectedRetirementDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return new WfpRetirementRisk
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            TenantId = tenantId,
            LocationCode = locationCode,
            SkillCode = skillCode,
            ExpectedRetirementDate = expectedRetirementDate,
        };
    }

    public void MarkRetired() => IsRetired = true;
}

/// <summary>
/// Configures the retirement age for a tenant, optionally scoped to a specific role.
/// Used by the workforce planning consumer to derive ExpectedRetirementDate from DateOfBirth
/// instead of using a global hardcoded constant.
/// Lookup order: exact (TenantId + RoleCode) → tenant default (TenantId + RoleCode IS NULL) → fallback constant (60).
/// </summary>
public sealed class WfpRetirementPolicy
{
    public const int FallbackRetirementAge = 60;

    private WfpRetirementPolicy() { TenantId = string.Empty; }

    public Guid Id { get; private set; }
    public string TenantId { get; private set; }
    /// <summary>Role this policy applies to. Null = default policy for all roles in this tenant.</summary>
    public string? RoleCode { get; private set; }
    /// <summary>Age (in years) at which employees in this tenant/role are expected to retire.</summary>
    public int RetirementAge { get; private set; }
    public bool IsActive { get; private set; } = true;

    public static WfpRetirementPolicy Create(string tenantId, int retirementAge, string? roleCode = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (retirementAge is < 40 or > 80)
            throw new ArgumentOutOfRangeException(nameof(retirementAge), "Retirement age must be between 40 and 80.");

        return new WfpRetirementPolicy
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RoleCode = roleCode?.Trim(),
            RetirementAge = retirementAge,
            IsActive = true,
        };
    }

    public void Deactivate() => IsActive = false;
}
