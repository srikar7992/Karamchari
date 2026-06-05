namespace Karamchari.TimeAttendance.Services;

using Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// Resolves the effective leave policy for an employee by traversing the hierarchy:
/// Global → Country → BusinessUnit → Department → Grade → Employee.
/// Most-specific non-null value wins at each field level.
/// Never call this from multiple places — all hierarchy logic lives here.
/// </summary>
public interface ILeavePolicyResolver
{
    Task<LeavePolicyResolution?> ResolveAsync(
        Guid employeeId,
        Guid leaveTypeId,
        DateOnly forDate,
        CancellationToken ct = default);
}

public sealed class LeavePolicyResolution
{
    public required Guid WinningPolicyId { get; init; }
    public required PolicyLevel WinningLevel { get; init; }
    public required LeavePolicyRules EffectiveRules { get; init; }
    public required decimal EffectiveEntitlement { get; init; }
}
