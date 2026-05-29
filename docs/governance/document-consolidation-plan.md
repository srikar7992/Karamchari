# Document Consolidation Plan

This document details the consolidation strategy for duplicate and overlapping documentation topics in the Karamchari repository.

---

## Topic Set 1: Local Setup & Onboarding
*   **Files**: `README-LOCAL.md`, `docs/engineering/local_setup.md`, `docs/governance/education/onboarding.md`, `docs/independence/new-developer-onboarding.md`
*   **Decisions**:
    *   `README-LOCAL.md`: **MERGE** and **DELETE**.
    *   `docs/engineering/local_setup.md`: **MERGE** and **DELETE**.
    *   `docs/governance/education/onboarding.md`: **KEEP** (Move to `docs/onboarding/engineer-program.md`).
    *   `docs/independence/new-developer-onboarding.md`: **KEEP** (Move to `docs/audits/historical/new-developer-onboarding.md`).
    *   `docs/development/local-setup/README.md`: **NEW** (Target source of truth).
*   **Justification**: Having setup steps in multiple files leads to drift. A single clean file in `docs/development/local-setup/README.md` will serve as the sole source of truth for host and Docker local setup.

---

## Topic Set 2: Multi-Tenancy & Tenant Isolation
*   **Files**: `docs/adr/0001-multi-tenancy-model.md`, `docs/adr/0002-row-level-security.md`, `docs/governance/mental_models/tenant_execution.md`, `docs/governance/explainability/tenant_execution_flow.md`
*   **Decisions**:
    *   `docs/adr/0001-multi-tenancy-model.md`: **KEEP** (Move to `docs/architecture/adrs/0001-multi-tenancy-model.md`).
    *   `docs/adr/0002-row-level-security.md`: **KEEP** (Move to `docs/architecture/adrs/0002-row-level-security.md`).
    *   `docs/governance/mental_models/tenant_execution.md`: **MERGE** and **DELETE**.
    *   `docs/governance/explainability/tenant_execution_flow.md`: **MERGE** and **DELETE**.
    *   `docs/architecture/decisions/tenant-execution.md`: **NEW** (Consolidated source of truth for tenant schemas, intercepts, and RLS).
*   **Justification**: ADRs preserve historical architectural intent. Runtime/mental models explain how tenant isolation runs at execution time. Merging explainability and mental models into a single file under `docs/architecture/decisions/` prevents duplication.

---

## Topic Set 3: Observability & Health Checks
*   **Files**: `docs/observability.md`, `docs/closure/observability.md`, `docs/independence/operations-observability.md`, `docs/health-checks.md`, `docs/closure/health-endpoints.md`
*   **Decisions**:
    *   `docs/observability.md`: **DELETE** (Redundant with closure details).
    *   `docs/closure/observability.md`: **MERGE** into `docs/operations/monitoring/observability.md` [NEW].
    *   `docs/independence/operations-observability.md`: **KEEP** (Move to `docs/audits/historical/operations-observability.md`).
    *   `docs/health-checks.md`: **DELETE**.
    *   `docs/closure/health-endpoints.md`: **MERGE** into `docs/operations/monitoring/health-checks.md` [NEW].
*   **Justification**: Observability setup and health checks represent live operational details. Keeping them separate under `docs/operations/monitoring/` makes it easy to look up how Seq, OTel, and HTTP health checks are configured.

---

## Topic Set 4: Messaging & Outbox
*   **Files**: `docs/outbox-audit.md`, `docs/certification/outbox.md`, `docs/closure/messaging-isolation.md`, `docs/independence/messaging-catalog.md`
*   **Decisions**:
    *   `docs/outbox-audit.md`: **DELETE**.
    *   `docs/certification/outbox.md`: **KEEP** (Move to `docs/audits/certification/outbox.md`).
    *   `docs/closure/messaging-isolation.md`: **KEEP** (Move to `docs/audits/historical/messaging-isolation-closure.md`).
    *   `docs/independence/messaging-catalog.md`: **KEEP** (Move to `docs/operations/monitoring/messaging-catalog.md`).
*   **Justification**: The `messaging-catalog.md` is the canonical source of truth for event/command topologies. Other audits/certifications are preserved under `docs/audits/` as historical compliance evidence.

---

## Topic Set 5: Database Registry & Contexts
*   **Files**: `docs/database-contexts.md`, `docs/independence/database-registry.md`
*   **Decisions**:
    *   `docs/database-contexts.md`: **DELETE**.
    *   `docs/independence/database-registry.md`: **KEEP** (Move to `docs/architecture/database-registry.md`).
*   **Justification**: The `database-registry.md` created in the handoff audit is more complete (cataloging table lists, retention times, and recovery categories for all 18 contexts). Keeping only one document avoids metadata drift.
