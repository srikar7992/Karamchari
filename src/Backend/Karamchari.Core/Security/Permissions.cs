namespace Karamchari.Core.Security;

/// <summary>
/// Centralized registry of platform permissions.
/// </summary>
public static class Permissions
{
    // Employee Domain
    public const string EmployeeRead = "employee.read";
    public const string EmployeeWrite = "employee.write";
    public const string EmployeeDelete = "employee.delete";

    // Payroll Domain
    public const string PayrollRead = "payroll.read";
    public const string PayrollRun = "payroll.run";
    public const string PayrollApprove = "payroll.approve";

    // Leave & Attendance
    public const string LeaveRead = "leave.read";
    public const string LeaveRequest = "leave.request";
    public const string LeaveApprove = "leave.approve";

    // Reimbursement
    public const string ReimbursementRead = "reimbursement.read";
    public const string ReimbursementSubmit = "reimbursement.submit";
    public const string ReimbursementApprove = "reimbursement.approve";

    // System/Tenant Admin
    public const string TenantSettingsRead = "tenant.settings.read";
    public const string TenantSettingsWrite = "tenant.settings.write";
    public const string AuditRead = "audit.read";

    // Capability Domain
    public const string CapabilityRead = "capability.read";
    public const string CapabilityWrite = "capability.write";

    // Billing Domain
    public const string BillingRead = "billing.read";
    public const string BillingWrite = "billing.write";

    public static readonly IReadOnlySet<string> All = new HashSet<string>
    {
        EmployeeRead, EmployeeWrite, EmployeeDelete,
        PayrollRead, PayrollRun, PayrollApprove,
        LeaveRead, LeaveRequest, LeaveApprove,
        ReimbursementRead, ReimbursementSubmit, ReimbursementApprove,
        TenantSettingsRead, TenantSettingsWrite,
        AuditRead,
        CapabilityRead, CapabilityWrite,
        BillingRead, BillingWrite
    };
}
