# Karamchari.Payroll

Payroll engine: pay periods and payroll runs with locking and approval, salary structure (components, templates, compensation profiles), calculation snapshots, earnings/deductions, tax rules and IT declarations, payslips, full-and-final settlements, arrears, corrections, reimbursements, loans, variable pay, salary revisions, disbursement batches, simulations, reconciliation jobs, and compliance filings.

Domain documentation: [docs/domains/Payroll.md](../../../../../docs/domains/Payroll.md)

## Domain ownership
Payroll calculation and disbursement. Compensation decisions belong to the Compensation module; leave/attendance inputs come from TimeAttendance events.

## Events published
Defined in `Karamchari.Payroll.Contracts` (`Contracts/*.cs`):

Events: `VariablePayApprovedIntegrationEvent`, `VariablePayPaidOutIntegrationEvent`, `SalaryRevisionApprovedIntegrationEvent`, `ReimbursementApprovedIntegrationEvent`, `ReimbursementPaidOutIntegrationEvent`, `LoanCreatedIntegrationEvent`, `LoanClosedIntegrationEvent`, `FnFSettlementInitiatedIntegrationEvent`, `FnFSettlementApprovedIntegrationEvent`, `FnFSettlementDisbursedIntegrationEvent`, `DisbursementBatchSubmittedIntegrationEvent`, `DisbursementBatchCompletedIntegrationEvent`, `DisbursementBatchFailedIntegrationEvent`, `CorrectionApprovedIntegrationEvent`, `CorrectionProcessedIntegrationEvent`, `ArrearCalculationApprovedIntegrationEvent`, `ArrearProcessedIntegrationEvent`

Commands: `StartPayrollRunCommand`, `LockPayrollRunCommand`, `CalculateAllEmployeePayCommand`, `ProcessPayrollBatchCommand`, `InitiateDisbursementCommand`, `RetryDisbursementCommand`, `InitiateFnFCalculationCommand`, `DisburseFnFCommand`, `DeductLoanInstallmentCommand`, `TriggerArrearCalculationCommand`, `TriggerCorrectionRecalculationCommand`

`PayrollRunCompletedIntegrationEvent` (Karamchari.Core.Contracts) is published from `Consumers/PayrollRunLockedConsumer.cs`; `ITDeclarationRejectedIntegrationEvent` from `Services/Declarations/ITDeclarationService.cs`.

## Events consumed
`EmployeeOnboardedIntegrationEvent`, `TenantProvisionedIntegrationEvent`, `TimesheetApprovedIntegrationEvent`, `LeaveRequestApprovedIntegrationEvent`, `WorkflowCompletedIntegrationEvent`, `PayrollRunLockedIntegrationEvent`, `PayrollRunCompletedIntegrationEvent` (payslip generation), `SalaryRevisionApprovedIntegrationEvent`, `ArrearCalculationApprovedIntegrationEvent`, plus its own commands listed above (see `Consumers/`).

## Database tables
Source of truth: `Data/PayrollDbContext.cs` and `Migrations/`. 42 sets, including:

`PayrollProfile`, `PayrollRun`, `PayrollRunState`, `PayPeriod`, `PayrollLock`, `CompensationProfile`, `OvertimePolicy`, `ShiftPremiumRule`, `PayrollCalculationSnapshot`, `PayrollEarning`, `TaxRuleVersion`, `DeductionRule`, `PayrollAdjustment`, `RetroPayrollAdjustment`, `Payslip`, `PayrollAuditEvent`, `EmployeePayrollResult`, `PayrollApproval`, `PayrollPublication`, `PayrollDeduction`, `PayrollSchedule`, `PayrollTimesheetLedger`, `SalaryComponent`, `SalaryTemplate`, `ProfessionalTaxSlab`, `ITDeclaration`, `PayrollLedgerEntry`, `ComplianceSnapshot`, `ComplianceFiling`, `FnFSettlement`, `FnFSettlementState`, `ArrearCalculation`, `PayrollCorrection`, `PayrollCorrectionState`, `ReimbursementClaim`, `EmployeeLoan`, `VariablePayAllocation`, `SalaryRevision`, `DisbursementBatch`, `DisbursementBatchState`, `PayrollSimulation`, `ReconciliationJob`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Payroll.Contracts`

## Wiring
Self-registered via `DependencyInjection/PayrollServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Payroll.Tests/Karamchari.Payroll.Tests.csproj
dotnet test tests/Backend/Karamchari.Payroll.IntegrationTests/Karamchari.Payroll.IntegrationTests.csproj
```
