namespace Karamchari.HR.Services;

/// <summary>
/// Resolves the set of employee IDs visible to a given actor based on active
/// org-graph relationships. Use this instead of Employee.ManagerId for all
/// manager-visibility decisions. See ADR-0011.
/// </summary>
public interface IVisibilityResolver
{
    /// <summary>
    /// Returns the set of employee IDs that <paramref name="actorId"/> can see
    /// under the requested <paramref name="scope"/>.
    /// Returns an empty set if the actor has no qualifying relationships.
    /// </summary>
    Task<IReadOnlySet<Guid>> ResolveVisibleEmployeeIdsAsync(
        Guid actorId,
        VisibilityScope scope,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Controls which relationship types are included when resolving visibility.
/// See ADR-0011 for the mapping table.
/// </summary>
public enum VisibilityScope
{
    /// <summary>DirectManager + ActingManager only.</summary>
    DirectOnly,

    /// <summary>DirectManager + ActingManager + DottedLineManager + ProjectManager.</summary>
    DirectAndFunctional,

    /// <summary>DirectManager + ActingManager + DottedLineManager + SkipLevelManager.</summary>
    SkipLevel,

    /// <summary>CalibrationCommitteeMember only.</summary>
    CalibrationPanel,

    /// <summary>All management types (excludes Mentor).</summary>
    AllManaged,
}
