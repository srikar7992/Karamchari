# Phase X: Canonical Workforce Domain Model

The Karamchari platform must operate on a unified, enterprise-wide definition of workforce entities. Modules may extend these entities via Domain Events or specific materialized views, but the source of truth remains canonical.

## 1. Foundation Entities
*   **`Tenant`**: The absolute boundary of data ownership and configuration.
*   **`Organization`**: The legal or logical business entity within a Tenant.
*   **`CostCenter`**: The financial bucket assigned to an Employee or Position.

## 2. Workforce Entities
*   **`Employee`**: The canonical identity of a person. (Eliminates separate "User", "Worker", "Staff" tables across modules).
*   **`Employment`**: The temporal contract binding an Employee to an Organization (supports re-hires, contract-to-FTE).
*   **`Position`**: The role an Employee occupies. Governs reporting lines and workflow assignments.
*   **`Department`**: The functional grouping of Positions.

## 3. Operational Entities
*   **`Shift`**: The defined working hours constraint.
*   **`Roster`**: The assigned future working schedule for an Employee or Department.
*   **`Attendance`**: The empirical record of an Employee's presence and time.
*   **`Leave`**: An authorized absence from a scheduled Shift or Roster.

## 4. Financial Entities
*   **`Compensation`**: The defined earning rules (Base, Variable, Allowances) attached to an Employment or Position.
*   **`PayrollArtifact`**: An immutable snapshot of earnings and deductions for a specific period (e.g., a generated Payslip).

## 5. Workflow Entities
*   **`Request`**: A polymorphic intent raised by an Employee (e.g., Leave Request, Reimbursement Claim, Salary Advance).
*   **`Approval`**: The recorded decision against a Request.
*   **`Policy`**: The rules engine configuration that validates a Request or dictates a Workflow.
*   **`Notification`**: The communication dispatched based on a Workflow state change.

## Module Boundaries
*   **HR**: Owns `Employee`, `Employment`, `Position`, `Department`.
*   **Time & Attendance**: Owns `Shift`, `Roster`, `Attendance`, `Leave`.
*   **Payroll**: Owns `Compensation`, `PayrollArtifact`.
*   **Workflow**: Owns `Request`, `Approval`, `Policy`, `Notification`.

Modules MUST reference entities outside their boundary by ID only, resolving materialized views via integration events, rather than duplicating the table structures.
