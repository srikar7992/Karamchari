# FinancialOps Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages workforce financial ledger entries, operational periods, loans, advances, compensation operations, reimbursement settlement batches, expense claims, operation events, and finalization snapshots. Evidence: `src/Backend/Karamchari.FinancialOps/Persistence/FinancialOpsDbContext.cs:32`. |
| Business Objectives | UNKNOWN beyond financial consistency, reimbursement settlement, and chaos survivability concepts exposed in code/tests. |
| Core Concepts | Ledger entry, operational period, loan, advance, compensation operation, settlement batch, expense claim, operation event, finalization snapshot. |
| Aggregates / Entities | DbSets in `FinancialOpsDbContext`. |
| Value Objects | Strongly typed IDs, currency code, financial correlation chain, replay metadata. Evidence: `src/Backend/Karamchari.FinancialOps.Contracts/Primitives/*.cs`. |
| State Machines | Financial operation status/type, expense claim status, settlement batch status, tax treatment, hold type. Evidence: `src/Backend/Karamchari.FinancialOps.Contracts/Primitives/FinancialOperationEnums.cs`, `src/Backend/Karamchari.FinancialOps.Contracts/Reimbursements/ReimbursementContracts.cs`, `src/Backend/Karamchari.FinancialOps.Contracts/Settlements/SettlementContracts.cs`. |
| Events | `ExpenseClaimSubmitted` and financial operation events. Evidence: `src/Backend/Karamchari.FinancialOps.Contracts/Reimbursements/ReimbursementContracts.cs:109`. |
| Commands | `SubmitExpenseClaim` consumed by `SubmitExpenseClaimConsumer`. Evidence: `src/Backend/Karamchari.FinancialOps.Contracts/Reimbursements/ReimbursementContracts.cs:91`, `src/Backend/Karamchari.FinancialOps/Domain/Reimbursements/SubmitExpenseClaimConsumer.cs:14`. |
| Queries | UNKNOWN; no BFF endpoints found. |
| Business Rules / Invariants / Validation | Consistency guard, execution sequence policy, replay service exist; exact invariants require source-level extraction. Evidence: `src/Backend/Karamchari.FinancialOps/Domain/Ledger/FinancialConsistencyGuard.cs`, `FinancialExecutionSequencePolicy.cs`, `FinancialReplayService.cs`. |
| Calculation Rules | UNKNOWN. |
| Ownership Rules | Tenant-scoped; finance authority UNKNOWN. |
| Dependencies | MassTransit, SQL, Payroll projections. Evidence: `src/Backend/Karamchari.FinancialOps.Contracts/Projections/PayrollProjections.cs`. |
| External Integrations | Manual settlement provider exists; real settlement provider UNKNOWN. Evidence: `src/Backend/Karamchari.FinancialOps/Domain/Settlements/ManualSettlementExecutionProvider.cs`. |
| Examples | Submit expense claim command only; public API UNKNOWN. |
| Failure Scenarios | Financial chaos tests exist. Evidence: `tests/Backend/Karamchari.FinancialChaosTests/*.cs`. |
| Known Limitations | No public endpoints found; operational support path UNKNOWN. |
