namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Specifies the type of edge relationship between nodes in the workforce graph.
/// </summary>
public enum WorkforceEdgeType
{
    /// <summary>OrgUnit to OrgUnit parent-child hierarchical relation.</summary>
    ParentOf,
    /// <summary>Position to Position reporting manager relationship line.</summary>
    ReportsTo,
    /// <summary>Employee to Position assignment occupancy relationship.</summary>
    Occupies,
    /// <summary>Employee to Skill validation node relationship.</summary>
    HasSkill,
    /// <summary>Goal to Goal organizational alignment path.</summary>
    AlignsTo,
    /// <summary>Employee to Position succession plan mapping.</summary>
    SuccessorFor,
    /// <summary>Employee to Opportunity marketplace enrollment path.</summary>
    EnrolledIn
}
