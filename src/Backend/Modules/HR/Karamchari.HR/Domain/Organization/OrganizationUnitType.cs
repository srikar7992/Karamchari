namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Categorizes organization units hierarchically.
/// </summary>
public enum OrganizationUnitType
{
    /// <summary>The parent company entity.</summary>
    Company,
    /// <summary>A broad line of business division.</summary>
    Division,
    /// <summary>A functional department.</summary>
    Department,
    /// <summary>A cross-functional team.</summary>
    Team,
    /// <summary>A small agile squad.</summary>
    Squad
}
