# BUSINESS JOURNEY OWNERSHIP MATRIX

**Date:** 2026-05-30
**Status:** ADOPTED

## Purpose

Cross-module business journeys require explicit ownership. This document designates a primary owner, supporting teams, escalation path, and certification authority for each platform journey.

---

## Journey 1: Employee Lifecycle

**Flow:** Identity → HR → Workflow → Payroll → Notification

| Role | Owner |
|---|---|
| **Primary Owner** | HR Module |
| **Supporting** | Identity (auth provisioning), Workflow (approval gates), Payroll (profile creation), Notifications (onboarding comms) |
| **Escalation Path** | HR → Platform Engineering → CTO |
| **Certification Authority** | HR Module Lead |

**Journey Definition:**
1. Identity: User account created, role assigned
2. HR: Employee record created, department/designation assigned
3. Workflow: Probation review gate (if configured)
4. Payroll: Payroll profile provisioned, salary structure assigned
5. Notification: Welcome email dispatched

**Certification Criteria:** Journey is certified when an employee can complete login, view their profile, and appear in a payroll run within 24 hours of onboarding.

---

## Journey 2: Leave Lifecycle

**Flow:** Leave → Approval → Manager → Attendance

| Role | Owner |
|---|---|
| **Primary Owner** | TimeAttendance Module |
| **Supporting** | Workflow (approval routing), HR (manager resolution), Notifications (approval/rejection comms) |
| **Escalation Path** | TimeAttendance → HR → Platform Engineering |
| **Certification Authority** | TimeAttendance Module Lead |

**Journey Definition:**
1. Employee submits leave request
2. Workflow routes to manager for approval
3. Manager approves/rejects
4. Leave balance debited on approval
5. Attendance records updated

**Certification Criteria:** Journey is certified when a leave request submitted by an employee results in a balance deduction and attendance record update within one processing cycle.

---

## Journey 3: Capability Lifecycle

**Flow:** Capability → Learning → Notification → Executive Dashboard

| Role | Owner |
|---|---|
| **Primary Owner** | Capability Module |
| **Supporting** | Notifications (learning reminders), Intelligence (dashboard aggregation) |
| **Escalation Path** | Capability → HR → CTO |
| **Certification Authority** | Capability Module Lead |

**Journey Definition:**
1. Admin defines capability gap or learning objective
2. Learning assignment dispatched to employee(s)
3. Notification sent to employee
4. Completion recorded
5. Executive dashboard updated with capability coverage

**Certification Criteria:** Journey is certified when a new capability maps to visible learning completions in the executive dashboard.

---

## Journey 4: Billing Lifecycle

**Flow:** Contract → Invoice → Payment → Collections

| Role | Owner |
|---|---|
| **Primary Owner** | Billing Module |
| **Supporting** | FinancialOps (payment processing), PSA (time-based billing inputs), Notifications (invoice delivery) |
| **Escalation Path** | Billing → Finance Lead → CFO |
| **Certification Authority** | Finance Lead |

**Journey Definition:**
1. Contract created and activated
2. Invoice generated (manual or time-driven)
3. Invoice dispatched to client
4. Payment received and reconciled
5. Overdue invoices escalated to collections

**Certification Criteria:** Journey is certified when a client contract generates a reconciled payment in the financial ledger without manual intervention.

---

## Journey 5: Recruitment Lifecycle

**Flow:** Candidate → Interview → Offer → Employee

| Role | Owner |
|---|---|
| **Primary Owner** | Recruitment Module |
| **Supporting** | Workflow (interview scheduling, approval gates), HR (employee record creation), Identity (account provisioning), Notifications (offer letters) |
| **Escalation Path** | Recruitment → HR → CTO |
| **Certification Authority** | HR Module Lead |

**Journey Definition:**
1. Candidate application received
2. Interview stages created and completed
3. Offer letter generated and dispatched
4. Candidate accepts offer
5. Employee record and identity account auto-created

**Certification Criteria:** Journey is certified when a candidate acceptance automatically triggers a complete employee onboarding (Journey 1) without manual data re-entry.

---

## Journey 6: Data Migration Lifecycle

**Flow:** Upload → Validate → Execute → Audit → Rollback

| Role | Owner |
|---|---|
| **Primary Owner** | DataMigration Module |
| **Supporting** | HR (employee reference resolution), TimeAttendance (leave balance writes), Payroll (salary structure writes), Notifications (import completion alerts) |
| **Escalation Path** | DataMigration → Platform Engineering → CTO |
| **Certification Authority** | Platform Engineering Lead |

**Journey Definition:**
1. Admin uploads import file
2. Dry-run validation produces an error report
3. Admin reviews and corrects source data
4. Import executed asynchronously
5. Audit trail written (who, when, rows, errors)
6. On failure: error report downloadable; records individually re-importable

**Certification Criteria:** Journey is certified when a 1000-row employee CSV produces a complete, isolated, auditable import result with zero cross-tenant contamination.

---

## Ownership Change Process

Journey ownership changes require:
1. Sign-off from the outgoing owner
2. Approval by Platform Engineering Lead
3. Update to this document in the same PR as the code change

Undocumented journey ownership is treated as a platform defect.
