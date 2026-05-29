# Phase Y: Operational Hardening - Audit Report
**Date:** 2026-05-11

## 1. State Ownership Report
Audit of modules for duplicated state and workflow-owned business truth violations.

| Module | Aggregate | Authoritative Business State | Duplicated/Workflow State |
| :--- | :--- | :--- | :--- |
| **Payroll** | `SalaryRevision` | `Applied`, `Cancelled` | `PendingApproval`, `Approved` (Duplicate of Workflow Coordination) |
| **TimeAttendance** | `LeaveRequest` | `Pending`, `Approved`, `Rejected` | Entire lifecycle mirrors workflow; domain logic is minimal. |
| **Performance** | `PromotionRecommendation` | `Draft`, `Approved`, `Withdrawn` | `ApprovedByManager`, `ApprovedByHR`, `ApprovedByLeadership` (Coordination leaking into Domain) |
| **Payroll** | `FnFSettlement` | `Initiated`, `Calculated`, `Disbursed` | (Requires further inspection of FnF state machine) |

## 2. Transition Ownership Report
Analysis of where lifecycle transitions are owned.

- **Payroll:** `SalaryRevision` owns `SubmitForApproval()` and `Approve()`. The `Approve()` method changes status to `Approved`. This should be a reaction to a successful workflow completion, not a direct call.
- **TimeAttendance:** `LeaveRequest` owns `Approve()` and `Reject()`. These are currently direct domain methods.
- **Performance:** `PromotionRecommendation` owns a multi-stage `Approve(stage, ...)` method. The domain aggregate is acting as a process engine.

## 3. Lifecycle Conflict Report
Identification of dual-source lifecycle management.

- **Dual Source Truth:** `PromotionRecommendation` has its own `PromotionStatus` which tracks stages. If a parallel `WorkflowInstance` exists, they can diverge.
- **Implicit Coordination:** `SalaryRevision` relies on manual `Approve()` calls which are often triggered by consumers. The "coordination" is scattered across consumers instead of centralized.
- **Business vs. Coordination:** `LeaveRequestStatus.Pending` is used both for business identification and for coordination wait-state.

## 4. Recommendations for Part 1
1. **Refactor SalaryRevision:** Status should be simplified to `Draft`, `Finalized` (replaces Approved/Applied), and `Cancelled`. Intermediate approval states move to Workflow.
2. **Refactor PromotionRecommendation:** Remove `PromotionStatus` stages. Domain aggregate should only care if it's `Proposed`, `Active`, or `Rejected`.
3. **Establish Workflow Callbacks:** Domains should listen for `WorkflowCompletedIntegrationEvent` to update their authoritative state.
