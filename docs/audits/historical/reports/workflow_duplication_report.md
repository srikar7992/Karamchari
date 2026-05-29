# Workflow Forensic Analysis: Duplication Report
**Date:** 2026-05-11
**Priority:** 3 — Workflow Convergence Preparation

## Executive Summary
Karamchari currently exhibits significant fragmentation in workflow orchestration, approval management, and lifecycle tracking. Each domain (HR, Payroll, Performance, Recruitment, TimeAttendance) maintains its own status enums, approval logic, and audit mechanisms. This report identifies these duplications to prepare for convergence into a unified platform workflow runtime.

## 1. Fragmented Lifecycle Statuses (Enums)
Almost every domain aggregate maintains an independent `Status` enum. While domain-specific states are sometimes necessary, many overlap with canonical workflow transitions (Pending, Approved, Rejected).

| Domain | Enum Name | States | Duplication Level |
|--------|-----------|--------|-------------------|
| **Payroll** | `RevisionStatus` | Draft, PendingApproval, Approved, Applied, Cancelled | High |
| **Payroll** | `LoanStatus` | Draft, PendingApproval, Active, Rejected, Cancelled | High |
| **Payroll** | `ReimbursementStatus` | Draft, Submitted, Approved, Rejected, Paid | High |
| **TimeAttendance** | `LeaveRequestStatus` | Pending, Approved, Rejected, Cancelled | High |
| **TimeAttendance** | `TimesheetStatus` | Draft, Submitted, Approved, Rejected | High |
| **Performance** | `PromotionStatus` | Draft, Submitted, UnderReview, ApprovedByManager, ApprovedByHR, ApprovedByLeadership, Approved, Rejected, Withdrawn | Critical (Hardcoded levels) |
| **Performance** | `GoalStatus` | Draft, PendingApproval, Active, Completed, Discarded | High |
| **Recruitment** | `OfferStatus` | Draft, PendingApproval, Sent, Accepted, Declined, Withdrawn | High |
| **Billing** | `InvoiceStatus` | Draft, Sent, Paid, Void, Overdue | Medium |

## 2. hardcoded Approval Logic
Several modules implement their own "approval chains" using hardcoded logic or multiple status flags, rather than a data-driven workflow engine.

- **Performance (Promotions):** Uses `PromotionStatus` to track sequential approval through Manager, HR, and Leadership.
- **Payroll (SalaryRevisions):** Implements `Approve(string approvedBy)` directly in the aggregate, bypassing centralized quorum or role validation.
- **TimeAttendance (Leaves):** Implements manual approval logic in application services.

## 3. Isolated Audit Mechanisms
Audit trails for transitions are inconsistent across modules.

- **Karamchari.Workflow:** Uses `WorkflowAuditEntry` (Action, Actor, StepOrder, StateSnapshot).
- **Karamchari.Performance:** Uses `PromotionApprovalAction` (Stage, Actor, Approved, Comments).
- **Karamchari.Core:** Uses a generic `AuditInterceptor` for property changes, but doesn't capture business-level "Transition Reason" consistently.

## 4. SLA and Deadline Tracking
SLA logic is partially standardized in `TenantExecutionEnvelope` but domain-specific deadlines remain isolated.

- **Capability:** `DeadlineUtc` on `LearningEnrollment`.
- **Performance:** `SubmissionDeadline` on Review Cycles.
- **Recruitment:** `ExpiryDate` on Offers.
- **Messaging:** `X-Workflow-Sla-Deadline` header enforced by `TenantConsumeFilter`.

## 5. Notification Triggers
Notifications for workflow events are manually triggered via Integration Events, leading to inconsistent messaging.

- **Goal Management:** `GoalApprovalRequiredConsumer` listens for domain events to send notifications.
- **Salary Revisions:** Triggers `SalaryRevisionApprovalRequestedEvent`.
- **Leave Management:** Custom logic in `LeaveService`.

## Recommendations for Convergence
1. **Adopt Canonical WorkflowStatus:** Transition all modules to use the standardized `WorkflowStatus` defined in `Karamchari.Core`.
2. **Unified Audit Entry:** Replace module-specific approval action tables with the platform-wide `WorkflowAuditEntry` structure.
3. **Standardized Events:** Consolidate all lifecycle events into the canonical `WorkflowTransitioned` and `WorkflowCompleted` events.
4. **Decouple Approval Logic:** Extract role/quorum validation from domain aggregates and delegate to the `Karamchari.Workflow` runtime.
