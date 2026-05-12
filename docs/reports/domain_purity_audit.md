# Phase Z: Domain Purity Audit Report
**Date:** 2026-05-11
**Objective:** Detect hidden coordination leakage in domain aggregates.

## 1. Domain Leakage Report
The following aggregates contain terminology or states that belong to the **Workflow Coordination** layer rather than **Business Truth**.

| Module | Aggregate | Leaked Terminology | Impact |
| :--- | :--- | :--- | :--- |
| **Payroll** | `EmployeeLoan` | `LoanStatus.PendingApproval` | Coordination state stored in domain. |
| **Payroll** | `VariablePayAllocation` | `VariablePayStatus.PendingApproval` | Coordination state stored in domain. |
| **Performance** | `CalibrationSession` | `CalibrationSessionStatus.PendingApproval` | Coordination state stored in domain. |
| **Performance** | `Goal` | `GoalStatus.PendingApproval` | Coordination state stored in domain. |
| **Recruitment** | `OfferLetter` | `OfferStatus.PendingApproval` | Coordination state stored in domain. |
| **Recruitment** | `JobRequisition` | `RequisitionStatus.PendingApproval` | Coordination state stored in domain. |
| **Billing** | `CollectionCase` | `CollectionStatus.Escalated` | Possible coordination leakage (Workflow vs Business Escalation). |

## 2. Workflow Leakage Heatmap
This heatmap identifies modules where the boundary between "What is true" (Domain) and "Who is doing what" (Workflow) is most blurred.

- **Recruitment Module (HIGH):** Entire lifecycle from Requisition to Offer relies on `PendingApproval` as a primary state driver.
- **Payroll Loans (MEDIUM):** Loan approval is a simple one-step coordination but is hardcoded into the loan aggregate.
- **Performance Goals (MEDIUM):** Goal activation is gated by a `PendingApproval` state within the domain.

## 3. Aggregate Purity Score
**SCORE: 65/100 (Substantial Coordination Leakage)**

While `SalaryRevision` and `LeaveRequest` were cleaned in Phase Y, the rest of the platform still follows the legacy pattern of treating "Waiting for someone" as a business status.

## 4. Remediation Plan
1. **Refactor Loans:** Move `PendingApproval` to `WorkflowInstance`. Aggregate state remains `Draft` until `FinalizeApproved()`.
2. **Refactor Goals:** Move `PendingApproval` to `WorkflowInstance`. Aggregate remains `Proposed` until `Activate()`.
3. **Refactor Recruitment:** Decouple `JobRequisition` and `OfferLetter` from their sequential approval states.
4. **Standardize Escalation:** Clarify if `CollectionStatus.Escalated` is a legal business state or a coordination timeout.
