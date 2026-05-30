# AUTHORIZATION MODEL REPORT

**Date:** 2026-05-30

## 1. Role Catalog
The platform defines the following static roles (source: `Roles.cs`):
- `SuperAdmin`: Platform-level administrator (bypasses all checks).
- `Admin`: Tenant-level administrator (bypasses permission checks within tenant).
- `Manager`: People manager with read/write access to team data.
- `Employee`: Standard user with read/write access to personal data.
- `ReadOnly`: Auditor/viewer with read-only access across the tenant.

## 2. Permission Catalog
The platform defines 18 fine-grained permissions (source: `Permissions.cs`):
- `employee.read`, `employee.write`, `employee.delete`
- `payroll.read`, `payroll.run`, `payroll.approve`
- `leave.read`, `leave.request`, `leave.approve`
- `reimbursement.read`, `reimbursement.submit`, `reimbursement.approve`
- `tenant.settings.read`, `tenant.settings.write`
- `audit.read`
- `capability.read`, `capability.write`
- `billing.read`, `billing.write`

## 3. The Gap (Root Cause)
At runtime, the Identity module successfully identifies user roles from the ASP.NET Identity store. However, during JWT generation in `IdentityEndpoints.cs`:

```csharp
// Current implementation (IdentityEndpoints.cs)
var token = await tokenService.GenerateAccessTokenAsync(
    user.TenantId!, 
    user.Id, 
    user.Email!, 
    roles, 
    []); // <--- EMPTY PERMISSIONS ARRAY HARDCODED
```

The permission set is explicitly passed as an empty array, regardless of the user's roles. Consequently:
- `PermissionRequirement` checks fail even for `Manager` or `Employee` roles.
- Endpoints like `GET /api/v1/capability/skills` (gated by `capability.read`) return **403 Forbidden**.

## 4. Intended Mapping (Remediation Target)

| Role | Permissions |
|---|---|
| `Admin` | ALL (bypasses via handler) |
| `Manager` | `*.read`, `*.write`, `*.approve`, `*.submit` |
| `Employee` | `employee.read`, `leave.read`, `leave.request`, `reimbursement.read`, `reimbursement.submit`, `capability.read` |
| `ReadOnly` | `*.read` |

GAP-2 remediation will implement this mapping via `IPermissionResolver` and inject it into the JWT.
