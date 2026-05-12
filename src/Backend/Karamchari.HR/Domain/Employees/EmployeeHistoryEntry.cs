using Karamchari.Core.Domain.Primitives;

namespace Karamchari.HR.Domain.Employees;

/// <summary>
/// An immutable entry in an employee's historical timeline.
/// Enables point-in-time reconstruction of organizational and lifecycle state.
/// </summary>
public sealed class EmployeeHistoryEntry : Entity<Guid>
{
    private EmployeeHistoryEntry() { }

    private EmployeeHistoryEntry(
        Guid id,
        Guid employeeId,
        EmployeeHistoryType type,
        string previousValue,
        string newValue,
        DateTimeOffset effectiveFrom,
        Guid changedBy,
        string? correlationId) : base(id)
    {
        EmployeeId = employeeId;
        Type = type;
        PreviousValue = previousValue;
        NewValue = newValue;
        EffectiveFrom = effectiveFrom;
        ChangedBy = changedBy;
        CorrelationId = correlationId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid EmployeeId { get; private set; }
    public EmployeeHistoryType Type { get; private set; }
    public string PreviousValue { get; private set; } = string.Empty;
    public string NewValue { get; private set; } = string.Empty;

    public DateTimeOffset EffectiveFrom { get; private set; }
    public DateTimeOffset? EffectiveTo { get; internal set; }

    public Guid ChangedBy { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static EmployeeHistoryEntry Create(
        Guid employeeId,
        EmployeeHistoryType type,
        string previousValue,
        string newValue,
        DateTimeOffset effectiveFrom,
        Guid changedBy,
        string? correlationId = null)
    {
        return new EmployeeHistoryEntry(
            Guid.NewGuid(), employeeId, type, previousValue, newValue,
            effectiveFrom, changedBy, correlationId);
    }
}
