# Saga Candidate Assessment (H-2)

**Status:** Discovery complete — implementation scoped, not started
**Date:** 2026-06-21
**Driver:** Certification finding **H-2** (`CERTIFICATION.md`) — no saga/compensation for multi-step cross-module flows.
**Method:** Map the real integration-event producer→consumer graph (from source), then test every cross-module workflow against four gates. Only flows that pass **all four** become sagas.

## The four gates
1. **Spans multiple modules?** (≥2 bounded contexts in one logical transaction)
2. **Does partial completion cause business damage?** (inconsistent/incorrect/unsafe state visible to users)
3. **Is rollback impossible?** (the first effect cannot be silently undone — a real-world fact was created)
4. **Is compensation required?** (a failed later step needs an explicit reversing action, not just a retry)

A workflow becomes a saga only if **1 AND 2 AND (3 OR 4)**. Notification-only fan-out and rebuildable projections fail gate 2 and stay as plain eventual events.

## Cross-module event graph (observed in source)
- **Recruitment** → `CandidateHired` → **HR** (creates Employee); Recruitment self-consumes analytics events.
- **HR** → `EmployeeOnboarded` → **Payroll, Benefits, Performance, Forecasting, Analytics, Intelligence**.
- **Payroll** → `PayrollRunCompleted` → **Compliance, Payroll**; disbursement & FnF chains → **Notifications**.
- **Benefits** → `BenefitEnrollmentActivated` / `BenefitElectionChanged` (→ payroll deduction relationship).
- **API/HR** → `EmployeeTerminated` → **AssetManagement, Helpdesk, Forecasting, Intelligence** (+ identity access revocation).
- **TimeAttendance** → `TimesheetApproved` → **Payroll, Billing, PSA, Compliance, Forecasting, Intelligence** (recompute/projection — idempotent).
- **Notifications**: pure sink for ~20 events (fire-and-forget).
- **Analytics / Intelligence / Forecasting**: projection sinks (eventual, rebuildable via `IProjectionRebuilder`).

## Assessment

| Workflow | Spans modules | Partial damage | Rollback impossible | Compensation needed | Verdict |
|---|---|---|---|---|---|
| **Hire → Employee → Payroll profile + Benefits enrollment** (CandidateHired→EmployeeOnboarded fan-out) | ✅ Recruitment→HR→Payroll/Benefits | ✅ employee exists but unpayable / unenrolled | ✅ employee is a real record | ✅ must back out or complete profile/enrollment | **SAGA — priority 1** |
| **Employee termination / offboarding** (EmployeeTerminated fan-out + access revocation) | ✅ HR/API→AssetMgmt/Helpdesk/Identity | ✅ terminated but access not revoked = **security risk**; assets not reclaimed | ✅ termination is a real event | ✅ ensure revoke + asset return complete or escalate | **SAGA — priority 2** |
| **Payroll disbursement batch & FnF settlement** (Submitted→Completed/Failed; FnF Initiated→Approved→Disbursed) | ✅ Payroll→Notifications/Finance | ✅ money moved / not moved inconsistently | ✅ disbursement is real money | ✅ reverse/refund on partial failure | **SAGA — priority 3** |
| **Benefits enrollment ↔ payroll deduction** (BenefitEnrollmentActivated → deduction) | ✅ Benefits→Payroll | ✅ enrolled but not deducted (or vice versa) | ⚠️ enrollment reversible if caught | ✅ compensate deduction/enrollment mismatch | **SAGA — priority 4** |
| Timesheet approved → Payroll/Billing recompute | ✅ | ❌ recompute is idempotent; no damage | ❌ | ❌ | Plain event (idempotent) |
| Leave approved/cancelled → Payroll | ✅ | ❌ recomputed | ❌ | ❌ | Plain event |
| Any event → Notifications | ✅ | ❌ missed notification is not data damage | n/a | ❌ | Plain event |
| Any event → Analytics/Intelligence/Forecasting projections | ✅ | ❌ rebuildable from source | ❌ | ❌ | Plain event |

## Recommended sagas (in priority order)
1. **EmployeeOnboardingSaga** — orchestrates CandidateHired → Employee created → Payroll profile created → Benefits enrolled. Compensation: if a downstream step fails terminally, raise `OnboardingIncompleteIntegrationEventV1` + flag the employee `ProvisioningIncomplete` (do not leave a silently half-provisioned employee). Timeout: escalate after N minutes.
2. **EmployeeOffboardingSaga** — EmployeeTerminated → access revocation (Identity) → asset return (AssetManagement) → final settlement (Payroll FnF). Compensation/guarantee: access revocation must be confirmed; unconfirmed revocation escalates to security. Timeout-driven.
3. **DisbursementSaga** — wrap the existing Submitted→Completed/Failed money chain with explicit compensation (reverse/hold) and a terminal reconciliation state.
4. **BenefitsEnrollmentSaga** — keep enrollment and the resulting payroll deduction consistent; compensate on mismatch.

## Implementation notes
- Use **MassTransit state-machine sagas** (already the transport); persist saga state in a dedicated `dbo`/per-tenant table; reuse the existing outbox for saga-published events.
- Every saga step must remain **idempotent** (consumers already are) and **tenant-scoped** (reuse `TenantConsumeFilter`).
- Add saga state + timeout handling; emit terminal success/failure integration events (versioned `…V1`, per the event-versioning standard).
- Do **not** convert the plain-event flows above — adding saga machinery there is cost without risk reduction.

## Next step
Approve this assessment, then implement priority-1 **EmployeeOnboardingSaga** first (highest business damage on partial completion), with an end-to-end test that kills a mid-step and asserts no half-provisioned employee remains.

## Implementation readiness (discovered while building P1)

| Saga | Status | Blocking reality |
|---|---|---|
| **P1 EmployeeOnboarding** | ✅ **Implemented + tested** | Both completion signals already existed (payroll profile, benefits enrollment). Shipped. |
| **P2 EmployeeOffboarding** | ⛔ Blocked — needs a new capability | `EmployeeTerminated` is consumed by AssetManagement/Helpdesk/Intelligence/Forecasting, but **Identity has NO access-revocation feature**. The saga's highest-value step (revoke access on termination) is unbuilt. Build the Identity access-revocation capability first, then orchestrate. Do not stub it. |
| **P3 Disbursement** | 🟡 Largely already a saga | Payroll already has `DisbursementBatchStateMachine` (Submitted→Completed/Failed). Remaining work is a **compensation audit** (explicit reverse/hold + terminal reconciliation), not a new saga. |
| **P4 BenefitsEnrollment↔deduction** | 🟡 Needs path verification | Confirm/implement the payroll-deduction-from-`BenefitEnrollmentActivated` path before wrapping it in a consistency saga. |

**Conclusion:** P1 was the only saga implementable as pure orchestration. P2 and P4 require new domain capabilities (a security feature and a deduction path); P3 is a hardening pass on existing saga code. These are feature-design tasks, not mechanical saga wiring, and should be scoped individually.
