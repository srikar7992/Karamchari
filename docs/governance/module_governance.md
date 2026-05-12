# Platform Module Governance
**Date:** 2026-05-11
**Status:** Active Execution Discipline

## 1. Module Classification (Priority 6, Step 1)
To ensure engineering focus remains on core product value and operational maturity, all platform modules are categorized below.

| Category | Description | Modules |
| :--- | :--- | :--- |
| **Core Operational** | Essential for daily business value. Active feature work allowed. | `HR`, `Payroll`, `TimeAttendance`, `Identity`, `Core`, `Api`, `Worker` |
| **Stabilization** | Feature-complete for Phase 1. Only stability and convergence work allowed. | `Workflow`, `Notifications`, `Performance`, `PSA`, `Recruitment`, `Capability`, `Compensation`, `Billing` |
| **Deferred (Frozen)** | Non-essential enterprise expansion. Feature freeze in effect. | `Governance`, `Intelligence`, `Forecasting` |
| **Experimental** | High-risk/speculative logic. Restricted to isolated sandboxes. | None (All currently integrated) |

## 2. Feature Freeze Enforcement (Priority 6, Step 2)
The following modules are officially under **Feature Freeze**. No new business capabilities, domain models, or orchestration logic may be added until Phase 2 maturity goals are met.

- **Karamchari.Governance:** Freeze all SLO, Schema, and Incident reporting expansions.
- **Karamchari.Intelligence:** Freeze all Signal, Metric, and Drift detection logic. No new AI/ML integrations.
- **Karamchari.Forecasting:** Freeze all payment profiling and cash-flow projection logic.

**Allowed Activities for Frozen Modules:**
- Critical bug fixes (security/data integrity).
- Documentation and XML comment improvements.
- Technical debt reduction (refactoring for convergence).
- Performance tuning.

## 3. Execution Governance Rules (Priority 6, Step 3)
Before proposing or implementing any new architectural abstraction, the following checklist must be satisfied:

1. **Operational Necessity:** What specific production incident or operational friction does this solve?
2. **Occam's Simplification:** Can this be achieved using existing primitives (e.g., standard MassTransit patterns, existing Interceptors)?
3. **Temporal Justification:** Why must this be built now instead of during Phase 2?
4. **Product Alignment:** Does this directly improve workforce ops, payroll, or attendance execution?

## 4. Product-First Priorities (Priority 6, Step 5)
Engineering effort must prioritize the following "North Star" product areas:

1. **Payroll Execution:** Zero-error processing, statutory compliance accuracy, and disbursement reliability.
2. **Workforce Operations:** Shift scheduling, attendance confidence scoring, and leave management.
3. **Employee Workflows:** Transparent approval chains and self-service efficiency.
4. **Tenant Isolation:** Guaranteed zero-leakage security and performance.
