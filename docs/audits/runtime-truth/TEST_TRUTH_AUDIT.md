# TEST TRUTH AUDIT

**Date**: 2026-06-02 (Round 1 initial + Round 2 corrected 2026-06-02)
**Program**: Sprint 1 + Sprint 2 Runtime Truth Program — Section 21
**Method**: `dotnet test --verbosity:minimal` actual run output, no estimates

---

## Test Discovery Counts — Round 2 Actual (Corrected)

> Round 1 cert doc numbers were aspirational. Round 2 re-ran every project and captured
> actual `Total` + `Passed` from `dotnet test` output. Numbers below are ground truth.

| Project | Type | Discovered | Passed | Skipped | Failed |
|---------|------|-----------|--------|---------|--------|
| Karamchari.Core.UnitTests | Platform Unit | 36 | 36 | 0 | 0 |
| Karamchari.Api.UnitTests | Platform Unit | 2 | 2 | 0 | 0 |
| Karamchari.ArchitectureTests | Architecture | 21 | 21 | 0 | 0 |
| Karamchari.Identity.Tests | Auth Unit | 34 | 34 | 0 | 0 |
| Karamchari.Recruitment.Tests | Domain Unit + Integration | 125 | 125 | 0 | 0 |
| Karamchari.HR.Tests | Domain Unit | 127 | 127 | 0 | 0 |
| Karamchari.DataMigration.Tests | API Cert + Unit | 326 | 275 | 51 | 0 |
| Karamchari.Billing.Tests | Domain Unit | 24 | 24 | 0 | 0 |
| Karamchari.Compensation.Tests | Domain Unit | 31 | 31 | 0 | 0 |
| Karamchari.FinancialOps.Tests | Domain Unit | 70 | 70 | 0 | 0 |
| Karamchari.Notifications.Tests | Domain Unit | 29 | 29 | 0 | 0 |
| Karamchari.Governance.Tests | Domain Unit | 20 | 20 | 0 | 0 |
| Karamchari.Workflow.Tests | Domain Unit | 17 | 17 | 0 | 0 |
| Karamchari.Intelligence.Tests | Domain Unit | 29 | 29 | 0 | 0 |
| Karamchari.Capability.Tests | Domain Unit | 36 | 36 | 0 | 0 |
| Karamchari.Forecasting.Tests | Domain Unit | 18 | 18 | 0 | 0 |
| Karamchari.Outbox.Tests | Infrastructure Unit | 40 | 40 | 0 | 0 |
| Karamchari.Payroll.Tests | Domain Unit | 44 | 44 | 0 | 0 |
| Karamchari.PSA.Tests | Domain Unit | 4 | 4 | 0 | 0 |
| Karamchari.TimeAttendance.Tests | Domain Unit | 1 | 1 | 0 | 0 |
| Karamchari.FinancialChaosTests | Chaos | 5 | 5 | 0 | 0 |
| Karamchari.TenantIsolationCertification | Tenant + Security | 628 | 626 | 2 | 0 |
| **TOTAL** | | **1,667** | **1,614** | **53** | **0** |

### Notes on Skipped Tests
- DataMigration.Tests: 51 skipped — `[Fact(Skip=...)]` on Testcontainers integration tests requiring Docker socket (CI-gated). All unit tests pass.
- TenantIsolationCertification: 2 skipped — Testcontainers tests requiring live SQL Server + RabbitMQ simultaneously.

### Round 1 vs Round 2 Discrepancy
Round 1 cert doc claimed 1,293 tests. Round 2 actual measurement: 1,614 passing. Discrepancy because:
1. `TenantIsolationCertification` (626 tests) was omitted entirely from Round 1 table
2. Several project counts were estimated high rather than measured (e.g., Api.UnitTests: claimed 40, actual 2)
3. DataMigration skipped tests were counted as passing in Round 1

## Round 2 New Tests Added (15 total)

| Test Class | Project | Tests Added | What It Proves |
|------------|---------|-------------|----------------|
| `InboxReplayIdempotencyTests` | HR.Tests | 4 | `CandidateHiredIntegrationEvent` ×10, ×50 concurrent → exactly 1 employee |
| `WorkflowRecoveryTests` | Recruitment.Tests | 4 | Offer lifecycle survives worker restart at each state transition |
| `IDORFixVerificationTests` | TenantIsolationCertification | 7 | PayrollRunState + SalaryRevision IDOR fixes verified; cross-tenant query returns null |

## False Test Findings Fixed

| Test | Old Behavior | Fix |
|------|-------------|-----|
| `ApplicationTests.RejectShouldBePossibleAfterHiring` | Asserted Reject succeeds after Hire (no domain guard existed) | Renamed + inverted: now asserts `InvalidOperationException` with `*terminal state*` |
| `HardProofTests.OutboxReliabilityTransactionCommit` | Asserted `OutboxMessage` table written with InMemory transport (impossible) | Removed impossible assertion; audit stream assertion kept |

## Coverage by Module

| Module | Test Projects | Coverage Focus |
|--------|--------------|----------------|
| Platform Core | UnitTests, ArchitectureTests, OutboxTests | Middleware, interceptors, outbox relay |
| Identity | Identity.Tests | Auth, JWT, RBAC, permission resolution |
| Recruitment | Recruitment.Tests | Domain lifecycle, concurrency, events, workflow recovery |
| HR | HR.Tests | Employee lifecycle, org hierarchy, inbox idempotency |
| DataMigration | DataMigration.Tests | Import pipeline (all 5 endpoints); 51 Docker-gated integration tests skipped |
| Billing | Billing.Tests | Contract, invoice domain |
| Compensation | Compensation.Tests | CTC, salary structures |
| FinancialOps | FinancialOps.Tests | Ledger, reconciliation |
| Notifications | Notifications.Tests | Digest, delivery orchestration |
| Governance | Governance.Tests | Policy engine |
| Workflow | Workflow.Tests | State machine, transitions |
| Intelligence | Intelligence.Tests | Signal processing, insights |
| Capability | Capability.Tests | Skills matrix |
| Forecasting | Forecasting.Tests | Workforce models |
| Payroll | Payroll.Tests | Payroll run cycle |
| PSA | PSA.Tests | Project resource allocation |
| TimeAttendance | TimeAttendance.Tests | Leave, attendance |
| Chaos | FinancialChaosTests | Financial resilience scenarios |
| Tenant + Security | TenantIsolationCertification | Schema isolation, RLS, IDOR fixes, red team scenarios |

---

## Verdict

**CERTIFIED** — 1,614 tests passing, 53 skipped (Docker-gated), 0 failed. Ground truth from `dotnet test` actual output. Round 2 corrects Round 1 estimated counts and adds 15 new tests covering inbox replay idempotency, workflow recovery, and IDOR fix verification.
