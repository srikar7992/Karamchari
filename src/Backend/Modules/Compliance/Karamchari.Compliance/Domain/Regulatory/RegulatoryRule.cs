using Karamchari.Compliance.Domain.Violations;
using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Compliance.Domain.Regulatory;

/// <summary>
/// A machine-evaluable rule within a regulatory register.
/// ConditionJson example: {"WeeklyHours":{"GreaterThan":48}}
/// </summary>
public sealed class RegulatoryRule : Entity<Guid>
{
    public Guid RegisterId { get; private set; }
    public string RuleCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string ConditionJson { get; private set; } = string.Empty;
    public decimal ViolationThreshold { get; private set; }
    public ViolationSeverity Severity { get; private set; }
    public bool Enabled { get; private set; }

    private RegulatoryRule() { }

    public static RegulatoryRule Create(
        Guid registerId,
        string ruleCode,
        string description,
        string conditionJson,
        decimal violationThreshold,
        ViolationSeverity severity)
    {
        return new RegulatoryRule
        {
            Id = Guid.NewGuid(),
            RegisterId = registerId,
            RuleCode = ruleCode,
            Description = description,
            ConditionJson = conditionJson,
            ViolationThreshold = violationThreshold,
            Severity = severity,
            Enabled = true
        };
    }
}
