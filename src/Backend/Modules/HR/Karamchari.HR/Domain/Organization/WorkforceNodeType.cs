namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Specifies the type of a node in the workforce graph representation.
/// </summary>
public enum WorkforceNodeType
{
    /// <summary>An organizational unit node.</summary>
    OrganizationUnit,
    /// <summary>A position node.</summary>
    Position,
    /// <summary>An employee node.</summary>
    Employee,
    /// <summary>A skill node.</summary>
    Skill,
    /// <summary>A goal node.</summary>
    Goal,
    /// <summary>A succession plan node.</summary>
    SuccessionPlan,
    /// <summary>An opportunity node.</summary>
    Opportunity,
    /// <summary>A competency node.</summary>
    Competency,
    /// <summary>A certification node.</summary>
    Certification,
    /// <summary>A designation node.</summary>
    Designation,
    /// <summary>A career path node.</summary>
    CareerPath
}
