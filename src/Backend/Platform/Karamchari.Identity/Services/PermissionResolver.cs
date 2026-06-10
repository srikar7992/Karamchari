// -----------------------------------------------------------------------
// <copyright file="PermissionResolver.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Security;

namespace Karamchari.Identity.Services;

/// <summary>
/// Implementation of <see cref="IPermissionResolver"/> that uses a static 
/// catalog mapping roles to permissions.
/// </summary>
public sealed class PermissionResolver : IPermissionResolver
{
    private const string CurrentVersion = "2026.05.30.2";

    public string GetPermissionVersion() => CurrentVersion;

    public IReadOnlyCollection<string> ResolvePermissions(IEnumerable<string> roles)
    {
        var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            var rolePermissions = role switch
            {
                Roles.Admin => Permissions.All,
                Roles.SuperAdmin => Permissions.All,
                Roles.Manager => new[]
                {
                    Permissions.EmployeeRead, Permissions.EmployeeWrite,
                    Permissions.PayrollRead, Permissions.PayrollRun,
                    Permissions.LeaveRead, Permissions.LeaveApprove,
                    Permissions.ReimbursementRead, Permissions.ReimbursementApprove,
                    Permissions.CapabilityRead, Permissions.CapabilityWrite,
                    Permissions.BillingRead, Permissions.BillingWrite,
                    Permissions.AuditRead,
                    Permissions.BulkImportRead
                },
                Roles.Employee => new[]
                {
                    Permissions.EmployeeRead,
                    Permissions.LeaveRead, Permissions.LeaveRequest,
                    Permissions.ReimbursementRead, Permissions.ReimbursementSubmit,
                    Permissions.CapabilityRead
                },
                Roles.ReadOnly => Permissions.All.Where(p => p.EndsWith(".read", StringComparison.OrdinalIgnoreCase)),
                Roles.Recruiter => new[]
                {
                    Permissions.RecruitmentRead, Permissions.RecruitmentCreate, Permissions.RecruitmentUpdate,
                    Permissions.RecruitmentInterview, Permissions.RecruitmentOffer, Permissions.RecruitmentHire,
                    Permissions.EmployeeRead,
                    Permissions.AuditRead
                },
                _ => Array.Empty<string>()
            };

            foreach (var p in rolePermissions)
            {
                permissions.Add(p);
            }
        }

        return permissions;
    }
}
