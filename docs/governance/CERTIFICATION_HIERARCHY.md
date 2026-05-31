# CERTIFICATION HIERARCHY

**Date:** 2026-05-30
**Purpose:** Establish the hierarchy of evidence for platform readiness.

## 1. Overview
The platform prioritizes business value over implementation details. Certification evidence is structured hierarchically, where higher levels depend on the successful execution of lower levels.

## 2. Evidence Hierarchy

### Level 3: Business Journey Certification (Primary)
- **Standard:** The end-to-end business process completes successfully.
- **Evidence:** Runtime proof of correct persistence, async messaging, authorization, and audit outcomes across multiple modules.
- **Example:** An employee is onboarded, their payroll profile is automatically created, a notification is sent to their manager, and the audit trail captures every step.

### Level 2: Module Certification (Secondary)
- **Standard:** The bounded context's core capabilities are operational.
- **Evidence:** Runtime proof that core module endpoints return successful responses (HTTP 20x) and maintain tenant isolation.
- **Example:** The `Payroll` module can successfully create and retrieve payroll runs.

### Level 1: Endpoint Certification (Supporting)
- **Standard:** Individual API operations adhere to their contract.
- **Evidence:** Automated tests or manual requests verifying status codes and schema correctness.
- **Example:** `GET /api/identity/login` returns `200 OK` with a valid JWT.

## 3. Certification Rules

1. **Hierarchy Rule:** A module is **not** certified merely because Level 1 (Endpoints) return HTTP 200. It must successfully contribute to a Level 3 (Business Journey).
2. **Outcome-First:** Certification focus is on business outcomes (e.g., "Is the data persisted in the correct tenant schema?") rather than implementation details (e.g., "Does the code look correct?").
3. **Continuous Requirement:** Certification must be re-verified continuously via automated CI/CD pipelines to prevent regression and drift.

## 4. Acceptance Summary
The platform status `FEATURE DEVELOPMENT READY` requires Level 2 certification for all existing modules and Level 3 certification for core lifecycles (Employee & Migration).
