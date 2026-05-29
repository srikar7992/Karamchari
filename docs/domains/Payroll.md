# Payroll Domain

## Evidence Boundary

Source: `src/Backend/Karamchari.Payroll`, contracts in `src/Backend/Karamchari.Payroll.Contracts`, payroll API endpoints in `src/Backend/Karamchari.Api/BFF/Payroll`, and tests in `src/Backend/Karamchari.Payroll.Tests`.

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages payroll profiles, runs, deductions, schedules, salary structures, statutory declarations, ledger, compliance, FnF, arrears, corrections, reimbursements, loans, variable pay, revisions, disbursement, simulations, reconciliation, and saga state. Evidence: `src/Backend/Karamchari.Payroll/Data/PayrollDbContext.cs:43`. |
| Business Objectives | UNKNOWN beyond running and locking payroll, generating payslips, compliance exports, and supporting payroll sub-flows exposed by endpoints and consumers. |
| Core Concepts | Payroll run, payroll profile, deduction, schedule, salary component/template, tax declaration, ledger entry, compliance filing, FnF settlement, arrear, correction, reimbursement, loan, variable pay, salary revision, disbursement batch, payroll simulation, reconciliation job. |
| Business Terminology | Payroll, payslip, salary component, CTC, statutory, EPF, ESIC, TDS, FnF, arrear, reimbursement, disbursement, reconciliation. Evidence: `src/Backend/Karamchari.Api/BFF/Payroll/*.cs`, `src/Backend/Karamchari.Api/BFF/Compliance/ComplianceEndpoints.cs:18`. |
| Aggregates / Entities | DbSets in `PayrollDbContext`: `PayrollProfile`, `PayrollRunState`, `PayrollDeduction`, `PayrollSchedule`, `PayrollTimesheetLedger`, `SalaryComponent`, `SalaryTemplate`, `ProfessionalTaxSlab`, `ITDeclaration`, `PayrollLedgerEntry`, `ComplianceSnapshot`, `ComplianceFiling`, `FnFSettlement`, `ArrearCalculation`, `PayrollCorrection`, `ReimbursementClaim`, `EmployeeLoan`, `VariablePayAllocation`, `SalaryRevision`, `DisbursementBatch`, `PayrollSimulation`, `ReconciliationJob`, saga states. Evidence: `src/Backend/Karamchari.Payroll/Data/PayrollDbContext.cs:43`. |
| Value Objects | Domain line items and installments exist, including `FnFLineItem`, `LoanInstallment`, `ArrearPeriodDiff`, `DisbursementEntry`. Evidence: `src/Backend/Karamchari.Payroll/Domain/FnF/FnFLineItem.cs`, `src/Backend/Karamchari.Payroll/Domain/Loans/LoanInstallment.cs`, `src/Backend/Karamchari.Payroll/Domain/Arrears/ArrearPeriodDiff.cs`, `src/Backend/Karamchari.Payroll/Domain/Disbursement/DisbursementEntry.cs`. |
| State Machines | Enums exist for arrears, corrections, disbursement, FnF, loans, reimbursements, salary structure, statutory verification, variable pay, reconciliation. Evidence: `src/Backend/Karamchari.Payroll/Domain/**/**/*Enums.cs` and saga-state DbSets in `PayrollDbContext`. |
| Events | Payroll contracts include arrear, correction, disbursement, FnF, loan, payroll run, reimbursement, and variable pay events. Evidence: `src/Backend/Karamchari.Payroll.Contracts/Contracts/*.cs`. |
| Commands | `StartPayrollRunCommand`, `LockPayrollRunCommand`, disbursement, FnF, arrear, correction, loan, reimbursement, and variable pay commands exist through API/consumer contracts. Evidence: `src/Backend/Karamchari.Core.Contracts/IntegrationEvents/IntegrationEvents.cs:105`, `src/Backend/Karamchari.Api/BFF/Payroll/*.cs`. |
| Queries | Payroll run list/summary/details plus all sub-flow list/get endpoints. Evidence: `src/Backend/Karamchari.Api/BFF/Payroll/PayrollEndpoints.cs:29`, `src/Backend/Karamchari.Api/BFF/Payroll/*Endpoints.cs`. |
| Business Rules / Invariants / Validation | `StartPayrollRunRequestValidator` exists. Other rules are embedded in domain methods and consumers; complete rule catalog is UNKNOWN. Evidence: `src/Backend/Karamchari.Api/Validation/StartPayrollRunRequestValidator.cs`, `src/Backend/Karamchari.Payroll/Domain`. |
| Calculation Rules | Salary template CTC/statutory endpoints exist; exact formulas are source-only and not independently documented. Evidence: `src/Backend/Karamchari.Api/BFF/Payroll/PayrollEndpoints.cs:38`, `src/Backend/Karamchari.Api/BFF/Payroll/PayrollEndpoints.cs:39`. |
| Ownership Rules | Tenant-scoped via shared tenant infrastructure; payroll-specific approval authority is UNKNOWN. |
| Dependencies | HR onboarding, TimeAttendance leave/timesheet events, Workflow completion, MassTransit, SQL outbox. Evidence: `EmployeeOnboardedConsumer`, `LeaveRequestApprovedConsumer`, `TimesheetApprovedConsumer`, `WorkflowCompletedPayrollConsumer` in `src/Backend/Karamchari.Payroll/Consumers`. |
| External Integrations | Bank disbursement abstraction exists; concrete production bank provider is UNKNOWN. Evidence: `src/Backend/Karamchari.Payroll/Domain/Disbursement/IBankDisbursementAdapter.cs`. |
| Examples | `POST /api/payroll/runs`, `PUT /api/payroll/runs/{id}/lock`, `POST /api/v1/payroll/fnf/initiate`, `POST /api/v1/payroll/disbursements/initiate`. Evidence: payroll endpoints. |
| Failure Scenarios | Disbursement failed event exists; reconciliation/anomaly endpoints exist. Evidence: `DisbursementBatchFailedIntegrationEvent` in `src/Backend/Karamchari.Payroll.Contracts/Contracts/DisbursementIntegrationEvents.cs`, `src/Backend/Karamchari.Api/BFF/Payroll/PayrollCockpitEndpoints.cs:29`. |
| Known Limitations | Tests exist only under `src/Backend/Karamchari.Payroll.Tests`; risk coverage and mutation coverage are not certified. |
