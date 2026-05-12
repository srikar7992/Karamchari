namespace Karamchari.Core.Security;

/// <summary>
/// Centralized registry of platform roles.
/// </summary>
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string HRManager = "HRManager";
    public const string PayrollManager = "PayrollManager";
    public const string Approver = "Approver";
    public const string Employee = "Employee";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        SuperAdmin,
        TenantAdmin,
        HRManager,
        PayrollManager,
        Approver,
        Employee
    };
}
