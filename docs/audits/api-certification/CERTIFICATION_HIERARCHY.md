# CERTIFICATION HIERARCHY

**Date:** 2026-05-30
**Status:** ADOPTED

## Purpose

The platform must certify business value, not endpoint counts. This document defines the three-tier certification hierarchy and the acceptance criteria at each level.

---

## The Hierarchy

```
┌────────────────────────────────────────────────────┐
│           Business Journey Certification           │  ← Highest
│  A complete journey produces correct outcomes      │
└────────────────────────────────────────────────────┘
                         ↑
┌────────────────────────────────────────────────────┐
│              Module Certification                  │
│  All primary entities and workflows in a module    │
│  function correctly with authorization             │
└────────────────────────────────────────────────────┘
                         ↑
┌────────────────────────────────────────────────────┐
│             Endpoint Certification                 │  ← Supporting evidence only
│  An endpoint returns the expected HTTP status      │
└────────────────────────────────────────────────────┘
```

---

## Level 1: Endpoint Certification

**Definition:** An individual HTTP endpoint responds correctly to authenticated and unauthenticated requests.

**Evidence Required:**
- HTTP `2xx` for valid authenticated request
- HTTP `401` for unauthenticated request
- HTTP `403` for insufficient permission
- HTTP `404` for non-existent resource (correct tenant isolation)

**Certification Status:** Supporting evidence only. Endpoint certification alone does not certify a module or a journey.

**Who Certifies:** Automated integration tests in `Karamchari.Core.IntegrationTests` or module-level integration tests.

---

## Level 2: Module Certification

**Definition:** A module's primary domain entities and workflows function correctly end-to-end, including:
- CRUD operations on primary aggregates
- Authorization enforcement (correct role/permission gates)
- Tenant isolation (no cross-tenant data leakage)
- Messaging (events published and consumed correctly)
- Persistence (data written and readable after round-trip)

**A module is NOT certified merely because endpoints return HTTP 200.**

**Evidence Required:**
1. All Level 1 (Endpoint) certifications pass for the module
2. At least one complete domain workflow verified (e.g., create → update → retrieve)
3. Tenant isolation verified (same resource is invisible from a different tenant)
4. Permissions verified for each role (Admin, Manager, Employee, ReadOnly)
5. At least one messaging path verified (event published → consumed → side effect confirmed)

**Who Certifies:** Module integration tests + human runtime verification documented in a certification report.

**Current Module Certification Status:**

| Module | Level 2 Status |
|---|---|
| Identity | ✅ CERTIFIED |
| HR | ✅ CERTIFIED |
| Payroll | ✅ CERTIFIED |
| Workflow | ✅ CERTIFIED |
| TimeAttendance | ✅ CERTIFIED |
| Capability | ✅ CERTIFIED |
| Billing | ✅ CERTIFIED |
| PSA | ✅ CERTIFIED |
| DataMigration | 🟡 IN PROGRESS (Sprint 1) |
| Recruitment | 🔲 NOT STARTED |
| Compensation | 🔲 NOT STARTED |
| Intelligence | 🔲 NOT STARTED |
| Forecasting | 🔲 NOT STARTED |

---

## Level 3: Business Journey Certification

**Definition:** A complete cross-module business journey executes successfully and produces:
- Correct **persistence** (all expected records written)
- Correct **authorization** (all gates enforced throughout the flow)
- Correct **messaging** (all integration events delivered and consumed)
- Correct **audit outcomes** (journey is traceable end-to-end)

**A business journey is NOT certified merely because each module in the flow is individually certified.**

**Evidence Required:**
1. All modules involved in the journey are Level 2 certified
2. The journey is executed end-to-end in a running environment (not just unit/integration tests)
3. Every module's contribution to the journey produces the correct side effect
4. The journey is repeatable (idempotency or clear error handling on re-run)
5. Cross-tenant isolation is verified (another tenant cannot observe the journey's data)

**Who Certifies:** Platform Engineering Lead + designated journey owner (see BUSINESS_JOURNEY_OWNERSHIP.md).

**Current Journey Certification Status:**

| Journey | Status |
|---|---|
| Employee Lifecycle | 🟡 PARTIAL (HR → Payroll linkage pending) |
| Leave Lifecycle | 🟡 PARTIAL (balance import pending Sprint 1) |
| Data Migration Lifecycle | 🟡 IN PROGRESS (Sprint 1) |
| Capability Lifecycle | 🔲 NOT STARTED |
| Billing Lifecycle | 🔲 NOT STARTED |
| Recruitment Lifecycle | 🔲 NOT STARTED |

---

## Certification Gate Policy

A module or journey must reach its certification level before it is declared "production ready."

| Gate | Required Level |
|---|---|
| Feature Development Ready | Level 2 (Module) for all modules in scope |
| Production Certified | Level 3 (Journey) for all journeys in scope |

The current platform status is **Feature Development Ready** (Level 2 certified for core modules). It is **not yet Production Certified** (Level 3 journeys remain incomplete).

---

## What Does NOT Count as Evidence

The following are explicitly insufficient as certification evidence:

- A swagger/Scalar UI showing endpoints exist
- A passing `dotnet build`
- An HTTP 200 response from a health check endpoint
- A count of entities in a database table
- A count of rows matching between tenant schemas
- Unit tests that mock the database layer

Evidence must always originate from a live, running system or a verified integration test that uses a real database.
