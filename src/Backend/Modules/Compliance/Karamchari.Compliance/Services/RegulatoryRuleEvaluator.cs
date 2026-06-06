using System.Text.Json;
using Karamchari.Compliance.Domain.Regulatory;
using Karamchari.Compliance.Domain.Violations;
using Karamchari.Compliance.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Compliance.Services;

/// <summary>
/// Evaluates enabled RegulatoryRules against a named metric value.
/// ConditionJson format: {"MetricName":{"GreaterThan":48}}
/// Operators: GreaterThan, LessThan, Equals
/// When rule triggers, delegates to PolicyViolationDetectionService using rule's RuleCode and Severity.
/// </summary>
public sealed class RegulatoryRuleEvaluator
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<RegulatoryRuleEvaluator> _logger;

    public RegulatoryRuleEvaluator(ComplianceDbContext db, ILogger<RegulatoryRuleEvaluator> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates all enabled regulatory rules for the tenant against the given metric.
    /// Returns list of rule codes that fired.
    /// </summary>
    public async Task<IReadOnlyList<string>> EvaluateAsync(
        string tenantId,
        Guid employeeId,
        string metricName,
        decimal metricValue,
        DateTime periodDate,
        CancellationToken ct = default)
    {
        var rules = await _db.RegulatoryRules
            .Join(_db.RegulatoryRegisters,
                rule => rule.RegisterId,
                reg => reg.Id,
                (rule, reg) => new { rule, reg })
            .Where(x => x.reg.TenantId == tenantId && x.reg.IsActive && x.rule.Enabled)
            .Select(x => x.rule)
            .ToListAsync(ct);

        var fired = new List<string>();

        foreach (var rule in rules)
        {
            if (!TryEvaluateCondition(rule.ConditionJson, metricName, metricValue))
                continue;

            // Idempotent: don't create duplicate violation this month
            var monthStart = new DateTime(periodDate.Year, periodDate.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var exists = await _db.PolicyViolations.AnyAsync(
                v => v.TenantId == tenantId
                  && v.EmployeeId == employeeId
                  && v.PolicyCode == rule.RuleCode
                  && v.Status == ViolationStatus.Open
                  && v.DetectedAt >= monthStart
                  && v.DetectedAt < monthEnd, ct);

            if (exists) continue;

            var evidence = JsonSerializer.Serialize(new
            {
                MetricName = metricName,
                MetricValue = metricValue,
                Threshold = rule.ViolationThreshold,
                RuleCode = rule.RuleCode,
                Period = periodDate
            });

            var violation = PolicyViolation.Detect(tenantId, rule.RuleCode, employeeId, rule.Severity, evidence);
            _db.PolicyViolations.Add(violation);
            fired.Add(rule.RuleCode);

            _logger.LogInformation("Regulatory rule {RuleCode} fired for employee {EmployeeId}: {Metric}={Value}",
                rule.RuleCode, employeeId, metricName, metricValue);
        }

        if (fired.Count > 0)
            await _db.SaveChangesAsync(ct);

        return fired;
    }

    private static bool TryEvaluateCondition(string conditionJson, string metricName, decimal value)
    {
        try
        {
            using var doc = JsonDocument.Parse(conditionJson);
            if (!doc.RootElement.TryGetProperty(metricName, out var metricNode))
                return false;

            if (metricNode.TryGetProperty("GreaterThan", out var gtProp) && gtProp.TryGetDecimal(out var gt))
                return value > gt;

            if (metricNode.TryGetProperty("LessThan", out var ltProp) && ltProp.TryGetDecimal(out var lt))
                return value < lt;

            if (metricNode.TryGetProperty("Equals", out var eqProp) && eqProp.TryGetDecimal(out var eq))
                return value == eq;

            return false;
        }
        catch
        {
            return false;
        }
    }
}
