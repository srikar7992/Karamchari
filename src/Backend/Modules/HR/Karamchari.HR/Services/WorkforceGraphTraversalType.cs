namespace Karamchari.HR.Services;

/// <summary>
/// Standardized traversal type tag values for <c>karamchari_graph_traversal_latency_ms</c>
/// and <c>karamchari_graph_traversal_count_total</c> metrics.
/// Using these constants ensures consistent tag values across all callers.
/// </summary>
public static class WorkforceGraphTraversalType
{
    /// <summary>Resolving all direct and indirect reports beneath a manager position.</summary>
    public const string IndirectReports = "IndirectReports";

    /// <summary>Walking from a node up to the root of its hierarchy.</summary>
    public const string PathToRoot = "PathToRoot";

    /// <summary>Comparing employee skills against a target position skill requirements.</summary>
    public const string SkillGap = "SkillGap";

    /// <summary>Scoring internal open positions against an employee's skill coverage.</summary>
    public const string InternalMobility = "InternalMobility";

    /// <summary>Discovering successor candidates for a critical position.</summary>
    public const string SuccessorDiscovery = "SuccessorDiscovery";
}
