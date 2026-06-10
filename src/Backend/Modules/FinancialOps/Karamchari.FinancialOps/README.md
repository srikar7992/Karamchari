# Karamchari.FinancialOps

Workforce financial operations: financial entries and operational periods, employee loans and advances, compensation operations, expense claims with settlement batches, finalization snapshots, and an append-only financial operation event log.

Domain documentation: [docs/domains/FinancialOps.md](../../../../../docs/domains/FinancialOps.md)

## Domain ownership
Employee-facing financial operations outside the payroll run itself. Payroll calculation and disbursement belong to the Payroll module; client invoicing belongs to Billing.

## Events published
Contract types live in `Karamchari.FinancialOps.Contracts`. `Domain/Reimbursements/SubmitExpenseClaimConsumer.cs` publishes `ExpenseClaimSubmitted` after accepting a claim.

## Events consumed
| Message | Consumer |
|---|---|
| `SubmitExpenseClaim` (command) | `Domain/Reimbursements/SubmitExpenseClaimConsumer.cs` |

## Database tables
Source of truth: `Persistence/FinancialOpsDbContext.cs` and `Migrations/`.

- `WorkforceFinancialEntry`, `FinancialOperationalPeriod`
- `EmployeeLoan`, `EmployeeAdvance`
- `FinancialCompensationOperation`
- `ExpenseClaim`, `ExpenseCategory`, `ReimbursementSettlementBatch`
- `FinancialOperationEvent`, `FinancialFinalizationSnapshot`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.FinancialOps.Contracts`

## Wiring
Self-registered via `DependencyInjection/FinancialOpsServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.FinancialOps.Tests/Karamchari.FinancialOps.Tests.csproj
dotnet test tests/Backend/Karamchari.FinancialChaosTests/Karamchari.FinancialChaosTests.csproj
```
