# Karamchari API Authorization Surface Map

This catalog maps every protected endpoint route on the Karamchari platform to its required roles, permissions, tenant context, and code files.

---

## 1. Protected Endpoints Map

| Endpoint Route | Module / BFF | Required Tenant Context | Required Permission / Policy | Owner / Target | Source File Reference |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `/api/v1/hr/employees` | HR BFF | Yes (Scoped) | Scoped (Authentication) | HR Admin | [EmployeeEndpoints.cs](src/Backend/Karamchari.Api/BFF/Employee/EmployeeEndpoints.cs) |
| `/api/v1/hr` | HR Workspace | Yes (Scoped) | Scoped (Authentication) | HR Admin | [HRWorkspaceEndpoints.cs](src/Backend/Karamchari.Api/BFF/HR/HRWorkspaceEndpoints.cs) |
| `/api/v1/hr/reports` | HR Reports | Yes (Scoped) | Scoped (Authentication) | HR Admin | [ExportJobEndpoints.cs](src/Backend/Karamchari.Api/BFF/HR/ExportJobEndpoints.cs) |
| `/api/v1/me` | Employee Workspace | Yes (Scoped) | Scoped (Authentication) | Employee | [EmployeeWorkspaceEndpoints.cs](src/Backend/Karamchari.Api/BFF/Employee/EmployeeWorkspaceEndpoints.cs) |
| `/api/v1/ops/dashboards` | Executive BFF | Yes (Scoped) | Scoped (Authentication) | Executive | [OperationsDashboardEndpoints.cs](src/Backend/Karamchari.Api/BFF/Executive/OperationsDashboardEndpoints.cs) |
| `/api/v1/executive` | Executive Workspace | Yes (Scoped) | Scoped (Authentication) | Executive | [ExecutiveWorkspaceEndpoints.cs](src/Backend/Karamchari.Api/BFF/Executive/ExecutiveWorkspaceEndpoints.cs) |
| `/api/v1/manager` | Manager BFF | Yes (Scoped) | Scoped (Authentication) | Manager | [ManagerEndpoints.cs](src/Backend/Karamchari.Api/BFF/Manager/ManagerEndpoints.cs) |
| `/api/v1/notifications` | Notifications | Yes (Scoped) | Scoped (Authentication) | General User | [NotificationCenterEndpoints.cs](src/Backend/Karamchari.Api/BFF/Notifications/NotificationCenterEndpoints.cs) |
| `/api/psa` | PSA BFF | Yes (Scoped) | Scoped (Authentication) | Project Manager | [PSAEndpoints.cs](src/Backend/Karamchari.Api/BFF/PSA/PSAEndpoints.cs) |
| `/api/analytics` | PSA Analytics | Yes (Scoped) | Scoped (Authentication) | Project Manager | [PSAEndpoints.cs](src/Backend/Karamchari.Api/BFF/PSA/PSAEndpoints.cs) |
| `/api/billing` | Billing BFF | Yes (Scoped) | Scoped (Authentication) | Billing Admin | [BillingEndpoints.cs](src/Backend/Karamchari.Api/BFF/Billing/BillingEndpoints.cs) |
| `/api/collections` | Collections | Yes (Scoped) | Scoped (Authentication) | Collections Admin | [BillingEndpoints.cs](src/Backend/Karamchari.Api/BFF/Billing/BillingEndpoints.cs) |
| `/api/forecast` | Forecasting | Yes (Scoped) | Scoped (Authentication) | Planning Admin | [BillingEndpoints.cs](src/Backend/Karamchari.Api/BFF/Billing/BillingEndpoints.cs) |
| `/api/ess` | Employee Self-Serv | Yes (Scoped) | Scoped (Authentication) | Employee | [ESSEndpoints.cs](src/Backend/Karamchari.Api/BFF/ESS/ESSEndpoints.cs) |
| `/api/v1/payroll/simulations`| Payroll Simulations| Yes (Scoped) | Scoped (Authentication) | Payroll Admin | [SimulationEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/SimulationEndpoints.cs) |
| `/api/v1/payroll/variable-pay`| Variable Pay | Yes (Scoped) | Scoped (Authentication) | Payroll Admin | [VariablePayEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/VariablePayEndpoints.cs) |
| `/api/payroll` | Payroll BFF | Yes (Scoped) | Scoped (Authentication) | Payroll Admin | [PayrollEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/PayrollEndpoints.cs) |
| `/api/v1/payroll/revisions` | Salary Revisions | Yes (Scoped) | Scoped (Authentication) | Payroll Admin | [SalaryRevisionEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/SalaryRevisionEndpoints.cs) |
| `/api/v1/payroll/reimbursements`| Reimbursements | Yes (Scoped) | Scoped (Authentication) | Employee / Admin | [ReimbursementEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/ReimbursementEndpoints.cs) |
| `/api/v1/payroll/loans` | Loans | Yes (Scoped) | Scoped (Authentication) | Employee / Admin | [LoanEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/LoanEndpoints.cs) |
| `/api/v1/approvals` | Approvals | Yes (Scoped) | Scoped (Authentication) | Manager / Admin | [ApprovalEndpoints.cs](src/Backend/Karamchari.Api/BFF/Common/ApprovalEndpoints.cs) |
| `/api/v1/payroll/disbursements`| Disbursements | Yes (Scoped) | Scoped (Authentication) | Finance Admin | [DisbursementEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/DisbursementEndpoints.cs) |
| `/api/v1/payroll/cockpit` | Payroll Cockpit | Yes (Scoped) | Scoped (Authentication) | Payroll Admin | [PayrollCockpitEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/PayrollCockpitEndpoints.cs) |
| `/api/compliance` | Compliance | Yes (Scoped) | Scoped (Authentication) | Compliance Officer|[ComplianceEndpoints.cs](src/Backend/Karamchari.Api/BFF/Compliance/ComplianceEndpoints.cs) |
| `/api/admin` | Platform Admin | Yes (Platform) | Scoped (Authentication) | Platform Admin | [ComplianceEndpoints.cs](src/Backend/Karamchari.Api/BFF/Compliance/ComplianceEndpoints.cs) |
| `/api/v1/payroll/fnf` | Full & Final (FnF) | Yes (Scoped) | Scoped (Authentication) | HR / Payroll Admin | [FnFEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/FnFEndpoints.cs) |
| `/api/v1/capability` | Capability Read | Yes (Scoped) | `capability.read` | General User | [CapabilityEndpoints.cs](src/Backend/Karamchari.Api/BFF/Capability/CapabilityEndpoints.cs) |
| `/api/v1/search` | Search | Yes (Scoped) | Scoped (Authentication) | General User | [SearchEndpoints.cs](src/Backend/Karamchari.Api/BFF/Search/SearchEndpoints.cs) |
| `/api/v1/strategy` | Intelligence Strat | Yes (Scoped) | Scoped (Authentication) | Executive | [StrategyEndpoints.cs](src/Backend/Karamchari.Api/BFF/Intelligence/StrategyEndpoints.cs) |
| `/api/v1/payroll/arrears` | Arrears | Yes (Scoped) | Scoped (Authentication) | Payroll Admin | [ArrearEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/ArrearEndpoints.cs) |
| `/api/v1/payroll/corrections` | Corrections | Yes (Scoped) | Scoped (Authentication) | Payroll Admin | [CorrectionEndpoints.cs](src/Backend/Karamchari.Api/BFF/Payroll/CorrectionEndpoints.cs) |
| `/api/v1/workforce/attendance`| Workforce Attendance| Yes (Scoped) | Scoped (Authentication) | Workforce Admin | [AttendanceEndpoints.cs](src/Backend/Karamchari.Api/BFF/Attendance/AttendanceEndpoints.cs) |
| `/api/v1/workforce/rosters` | Workforce Rosters | Yes (Scoped) | Scoped (Authentication) | Workforce Admin | [AttendanceEndpoints.cs](src/Backend/Karamchari.Api/BFF/Attendance/AttendanceEndpoints.cs) |
| `/api/v1/time` | Timesheets | Yes (Scoped) | Scoped (Authentication) | Employee | [AttendanceEndpoints.cs](src/Backend/Karamchari.Api/BFF/Attendance/AttendanceEndpoints.cs) |
| `/api/v1/leaves` | Leaves | Yes (Scoped) | Scoped (Authentication) | Employee / Manager | [LeaveEndpoints.cs](src/Backend/Karamchari.Api/BFF/Attendance/LeaveEndpoints.cs) |
