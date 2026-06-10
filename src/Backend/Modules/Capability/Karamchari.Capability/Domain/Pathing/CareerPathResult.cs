using System;
using System.Collections.Generic;
using Karamchari.Capability.Domain.Primitives;

namespace Karamchari.Capability.Domain.Pathing;

/// <summary>
/// Query model returned by <see cref="Karamchari.Capability.Services.CareerPathService"/>.
/// Derived at query time from existing Gap and Readiness projections — not persisted.
/// </summary>
public sealed class CareerPathResult
{
    public Guid EmployeeId { get; init; }
    public Guid TargetRoleRequirementId { get; init; }
    public string TargetRoleTitle { get; init; } = string.Empty;

    /// <summary>Employee's current readiness band against the target role.</summary>
    public CareerReadinessBand CurrentBand { get; init; }

    public decimal CurrentReadinessScore { get; init; }
    public decimal CurrentCoveragePercent { get; init; }
    public int MissingSkillCount { get; init; }
    public int CriticalGapCount { get; init; }

    /// <summary>
    /// Ordered skill acquisition steps to reach ReadyNow in the target role.
    /// Critical gaps first, then shallowest gaps. Empty when employee is already ReadyNow.
    /// </summary>
    public IReadOnlyList<CareerPathStep> Steps { get; init; } = [];

    /// <summary>
    /// Role hops from a ReadyNow origin role to the target via progression edges.
    /// Empty when no graph path exists or employee has no ReadyNow role.
    /// </summary>
    public IReadOnlyList<CareerProgressionHop> Path { get; init; } = [];
}

/// <summary>
/// A single skill the employee must acquire or improve on the path to the target role.
/// Priority: 1 = missing entirely, 2 = critical gap (2+ levels behind), 3 = shallow gap.
/// </summary>
public sealed record CareerPathStep(
    Guid SkillId,
    string SkillName,
    SkillLevel RequiredLevel,
    SkillLevel? CurrentLevel,
    int LevelsBehind,
    bool IsCritical,
    int Priority);

/// <summary>A single role on the career path from an origin role to the target role.</summary>
public sealed record CareerProgressionHop(
    Guid RoleRequirementId,
    string RoleTitle,
    int StepIndex);
