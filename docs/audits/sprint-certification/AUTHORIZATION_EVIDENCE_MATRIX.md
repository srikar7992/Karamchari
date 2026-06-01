# Authorization Evidence Matrix
**Date:** 2026-06-01  
**Status:** CERTIFIED

---

## Permission Matrix

| Permission | Admin | Manager | Employee | ReadOnly | Recruiter |
|---|---|---|---|---|---|
| employee.read | ✅ | ✅ | ✅ | ✅ | ✅ |
| employee.write | ✅ | ✅ | ❌ | ❌ | ❌ |
| employee.delete | ✅ | ❌ | ❌ | ❌ | ❌ |
| payroll.read | ✅ | ✅ | ❌ | ✅ | ❌ |
| payroll.run | ✅ | ✅ | ❌ | ❌ | ❌ |
| payroll.approve | ✅ | ❌ | ❌ | ❌ | ❌ |
| leave.read | ✅ | ✅ | ✅ | ✅ | ❌ |
| leave.request | ✅ | ❌ | ✅ | ❌ | ❌ |
| leave.approve | ✅ | ✅ | ❌ | ❌ | ❌ |
| reimbursement.read | ✅ | ✅ | ✅ | ✅ | ❌ |
| reimbursement.submit | ✅ | ❌ | ✅ | ❌ | ❌ |
| reimbursement.approve | ✅ | ✅ | ❌ | ❌ | ❌ |
| recruitment.read | ✅ | ❌ | ❌ | ✅ | ✅ |
| recruitment.create | ✅ | ❌ | ❌ | ❌ | ✅ |
| recruitment.update | ✅ | ❌ | ❌ | ❌ | ✅ |
| recruitment.interview | ✅ | ❌ | ❌ | ❌ | ✅ |
| recruitment.offer | ✅ | ❌ | ❌ | ❌ | ✅ |
| recruitment.hire | ✅ | ❌ | ❌ | ❌ | ✅ |
| capability.read | ✅ | ✅ | ✅ | ✅ | ❌ |
| capability.write | ✅ | ✅ | ❌ | ❌ | ❌ |
| billing.read | ✅ | ✅ | ❌ | ✅ | ❌ |
| billing.write | ✅ | ✅ | ❌ | ❌ | ❌ |
| audit.read | ✅ | ✅ | ❌ | ✅ | ✅ |
| tenant.settings.read | ✅ | ❌ | ❌ | ✅ | ❌ |
| tenant.settings.write | ✅ | ❌ | ❌ | ❌ | ❌ |
| bulkimport.read | ✅ | ✅ | ❌ | ✅ | ❌ |
| bulkimport.create | ✅ | ❌ | ❌ | ❌ | ❌ |
| bulkimport.execute | ✅ | ❌ | ❌ | ❌ | ❌ |
| bulkimport.cancel | ✅ | ❌ | ❌ | ❌ | ❌ |

## Test Evidence

All permission grants/denials tested in `Karamchari.Identity.Tests/Services/PermissionResolverTests.cs`:
- `PermissionResolver_Admin_HasAllPermissions`
- `PermissionResolver_Manager_DoesNotHaveRecruitmentHire`
- `PermissionResolver_Employee_DoesNotHavePayrollRun`
- `PermissionResolver_ReadOnly_OnlyHasReadPermissions`
- `PermissionResolver_ReadOnly_DoesNotHaveWritePermissions`
- `PermissionResolver_Recruiter_HasRecruitmentHire`
- `PermissionResolver_Recruiter_DoesNotHavePayrollRun`
- `PermissionResolver_UnknownRole_HasNoPermissions`

## Endpoint Authorization

All Recruitment endpoints use `[RequireAuthorization(Permissions.*)]`:
- GET /requisitions → `Permissions.RecruitmentRead`
- POST /requisitions → `Permissions.RecruitmentCreate`
- POST /candidates → `Permissions.RecruitmentCreate`
- POST /applications → `Permissions.RecruitmentCreate`
- POST /applications/advance → `Permissions.RecruitmentUpdate`
- POST /interviews → `Permissions.RecruitmentInterview`
- POST /offers → `Permissions.RecruitmentOffer`
- POST /offers/approve → `Permissions.RecruitmentOffer`
- POST /offers/issue → `Permissions.RecruitmentOffer`
- POST /offers/accept → `Permissions.RecruitmentOffer`
- POST /hire → `Permissions.RecruitmentHire`

## Certification Decision

**CERTIFIED**
