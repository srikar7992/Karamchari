namespace Karamchari.HR.Domain.Employees;

/// <summary>
/// Defines the types of historical changes tracked for an employee.
/// </summary>
public enum EmployeeHistoryType
{
    /// <summary>Change in reporting manager.</summary>
    ManagerChange = 1,

    /// <summary>Transfer between departments.</summary>
    DepartmentTransfer = 2,

    /// <summary>Change in job designation/title.</summary>
    DesignationChange = 3,

    /// <summary>Transfer between physical branches.</summary>
    BranchTransfer = 4,

    /// <summary>Transfer between legal entities (e.g. inter-company transfer).</summary>
    LegalEntityTransfer = 5,

    /// <summary>Change in employment type (e.g. Contract to Full-Time).</summary>
    EmploymentTypeChange = 6,

    /// <summary>Change in business lifecycle state.</summary>
    LifecycleStateChange = 7,

    /// <summary>Change in work time zone.</summary>
    TimeZoneChange = 8
}
