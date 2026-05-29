# Phase 7: Database Ownership Audit & Registry

This document lists and details every `DbContext` implementation in the Karamchari platform to establish ownership, schema structures, tenant boundaries, and operational recovery needs.

---

## Global Database Architecture

- **Database Engine**: Azure SQL Database (deployed in SQL Elastic Pools for cost/resource scaling).
- **Tenancy Isolation Model**: Shared Database, Isolated Schemas (`tenant_<tenantId>`).
- **Dynamic Schema Isolation**: Inherited from [KaramchariDbContext](file:///Users/srikarbojji/Projects/Karamchari/src/Backend/Karamchari.Core/Persistence/KaramchariDbContext.cs), which maps all models to the placeholder schema `__tenant__` at compile-time. The [TenantSchemaCommandInterceptor](file:///Users/srikarbojji/Projects/Karamchari/src/Backend/Karamchari.Core/Persistence/Interceptors/TenantSchemaCommandInterceptor.cs) rewrites this placeholder to the active tenant's schema before execution.
- **Defensive Row-Level Security**: A session context interceptor writes `TenantId` on open connections, and SQL Server Row-Level Security (RLS) policies filter records as a failsafe backup.

---

## Database Ownership Registry

### 1. Platform Infrastructure Contexts

#### CoreDbContext
- **Namespace**: `Karamchari.Core.Persistence`
- **Owner Module**: Core Platform (`Karamchari.Core`)
- **Tenant Strategy**: Shared `dbo` (Shared infrastructure tables)
- **Primary Tables**: `dbo.IdempotentRequests`, `dbo.AuditLogs`
- **Migration Owner**: None (Database objects created by provisioning scripts)
- **Retention Rules**: Logs retained for 1 Year; Idempotent request signatures cleared after 7 days.
- **Backup Requirements**: Captured during standard database-level backups (daily full, hourly diffs).

#### OutboxRelayDbContext
- **Namespace**: `Karamchari.Core.Messaging.Outbox`
- **Owner Module**: Core Platform (`Karamchari.Core`)
- **Tenant Strategy**: Shared `dbo`
- **Primary Tables**: `dbo.OutboxRelayState`
- **Migration Owner**: Core (`20260508031638_AddOutboxInfrastructure`)
- **Retention Rules**: State entries are persistent but transient in value (tracks current message offset).
- **Backup Requirements**: Low priority (relay state can be reconstructed by inspecting message broker offsets).

#### IdentityDbContext
- **Namespace**: `Karamchari.Identity.Infrastructure.Persistence`
- **Owner Module**: Identity & Auth Module (`Karamchari.Identity.Infrastructure`)
- **Tenant Strategy**: Shared `identity` schema
- **Primary Tables**: `identity.AspNetUsers`, `identity.AspNetRoles`, `identity.SigningKeys`, `identity.RefreshTokens`
- **Migration Owner**: Identity (`20260528044150_Initial`)
- **Retention Rules**: Indefinite while accounts are active.
- **Backup Requirements**: High priority (critical user identity and auth metadata).

---

### 2. Tenant Domain Contexts (Multi-Tenant Schema)

Every domain context inherits from `KaramchariDbContext` and runs within the active tenant's schema (`tenant_<tenantId>`).

| DbContext | Primary Tables | Owner Module | Retention Policy | Recovery Tier |
| :--- | :--- | :--- | :--- | :--- |
| **HRDbContext** | `Employees`, `Departments`, `EmployeeRelationships` | `Karamchari.HR` | Indefinite (Active staff) | Tier 1 (Critical) |
| **PayrollDbContext** | `PayrollRuns`, `SalaryStructures`, `ReimbursementClaims`, `SalaryRevisions`, `Loans`, `VariablePayComponents` | `Karamchari.Payroll` | Indefinite (Statutory compliance) | Tier 1 (Critical) |
| **TimeAttendanceDbContext** | `WorkShifts`, `AttendanceRecords`, `LeaveApplications`, `ProjectMetrics` | `Karamchari.TimeAttendance` | 3 Years | Tier 2 (High) |
| **PSADbContext** | `Projects`, `ProjectMembers`, `Timesheets`, `TimesheetLines`, `BillableEntries` | `Karamchari.PSA` | 5 Years | Tier 1 (Critical) |
| **PerformanceDbContext** | `Goals`, `KeyResults`, `ReviewCycles`, `ReviewAssignments`, `ReviewTaskInboxItems`, `TalentHeatmapEntries`, `PromotionPipelineItems`, `EmployeeSkillInventoryItems` | `Karamchari.Performance` | Indefinite | Tier 2 (High) |
| **NotificationsDbContext** | `NotificationDigests`, `PushNotificationSubscriptions`, `NotificationPreferences` | `Karamchari.Notifications` | 30 Days (Digests) | Tier 3 (Low) |
| **CompensationDbContext** | `CompensationBands`, `IncrementBudgetPools`, `StockGrantRecords` | `Karamchari.Compensation` | Indefinite | Tier 1 (Critical) |
| **RecruitmentDbContext** | `JobPostings`, `Applications`, `Interviews`, `Offers` | `Karamchari.Recruitment` | 2 Years (Applications) | Tier 2 (High) |
| **CapabilityDbContext** | `Skills`, `Roles`, `RoleSkillRequirements`, `Assessments` | `Karamchari.Capability` | Indefinite | Tier 2 (High) |
| **IntelligenceDbContext** | `PerformanceProjections`, `FlightRiskPredictions`, `SuccessionCandidates` | `Karamchari.Intelligence` | 1 Year (Projections) | Tier 3 (Low) |
| **GovernanceDbContext** | `CompliancePolicies`, `AuditLogs`, `AccessReviews` | `Karamchari.Governance` | 7 Years (Regulatory) | Tier 1 (Critical) |
| **BillingDbContext** | `Invoices`, `InvoiceLines`, `ClientAccounts`, `Payments` | `Karamchari.Billing` | 10 Years (Auditing) | Tier 1 (Critical) |
| **ForecastingDbContext** | `RevenueForecasts`, `ResourceDemands` | `Karamchari.Forecasting` | 1 Year (Recycle) | Tier 2 (High) |
| **WorkflowDbContext** | `WorkflowInstances`, `WorkflowTasks`, `WorkflowDefinitions` | `Karamchari.Workflow` | 6 Months (Completed runs) | Tier 2 (High) |
| **FinancialOpsDbContext** | `FinancialOperationalPeriods`, `LedgerEntries`, `FinalizationSnapshots`, `OperationEvents` | `Karamchari.FinancialOps` | Indefinite (Finance history) | Tier 1 (Critical) |

---

## Verdict: **PASS**

The database mapping model is exceptionally structured. All context definitions cleanly extend [KaramchariDbContext](file:///Users/srikarbojji/Projects/Karamchari/src/Backend/Karamchari.Core/Persistence/KaramchariDbContext.cs), standardizing RLS and schema isolated queries. 

Migrations are cleanly owned by their respective modules. Data recovery is simplified because all schemas reside in a single SQL Server database instance, permitting unified point-in-time restores (PITR).
