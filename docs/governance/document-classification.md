# Document Classification Report

This document classifies every markdown file in the repository into one of three categories: Active Source of Truth, Generated Report, or Obsolete.

---

## Category A: Active Source of Truth
These documents represent persistent, authoritative specifications, standards, runbooks, and design records. They are actively maintained and serve as the single source of truth for their respective topics.

| Path | Topic | Purpose |
| :--- | :--- | :--- |
| `README.md` | Platform Portal | Entrance point, high-level map, and workflows. |
| `docs/architecture/adrs/0001-multi-tenancy-model.md` to `0014-search-infrastructure.md` | Architectural Decisions | Decisive context behind patterns (RLS, Tenanting, BFF). |
| `docs/architecture/startup-sequence.md` | Bootstrapping Flow | Detailed sequence of middleware and DI initialization. |
| `docs/domains/*.md` | Business Domain | Domain concepts, models, and boundaries for each module. |
| `docs/domains/domain-dictionary.md` | Domain Dictionary | Core glossary of business terms. |
| `docs/development/standards/architecture-standards.md` | Architecture Standards | System design boundaries and rules. |
| `docs/development/standards/golden-paths.md` | Golden Paths | Guides for building components. |
| `docs/development/standards/platform-contracts.md` | System Contracts | Outlines event-driven messaging structures. |
| `docs/development/standards/reliability-standards.md` | Reliability Standards | SQL resiliency, exception schemas, and retry policies. |
| `docs/development/standards/workforce-temporal-standards.md` | Temporal Standards | Rules for managing Date/Time across schemas. |
| `docs/operations/recovery/replay-recovery.md` | Operations | Steps for recovering from failed async state machine transitions. |
| `docs/operations/runbooks/README.md` | Operations | Operational runbooks portal. |
| `docs/audits/historical/independence/runbooks.md` | Operations | Reconstructed recovery runbooks. |
| `docs/operations/monitoring/messaging-catalog.md` | System Messaging | Complete mapping of publishers, exchanges, and queues. |
| `docs/architecture/database-registry.md` | Database Ownership | Context schema ownership, tables, and backup tiers. |
| `docs/development/standards/commit-conventions.md` | Standards | Git flow commit syntax. |
| `docs/development/standards/analyzer-policy.md` | Standards | Code style and analyzer suppression permissions. |
| `docs/development/standards/package-governance.md` | Standards | Centralized package management rules. |
| `docs/development/standards/branching-strategy.md` | Standards | Git branch constraints and protection rules. |
| `docs/development/standards/ci-cd-policy.md` | Standards | Policy governing pipeline execution and triggers. |
| `docs/reference/api-explorer.md` | API Explorer | Configuration for Scalar interactive API explorer. |

---

## Category B: Generated Report
These documents are historical artifacts, certifications, compliance checklists, or hostile audit results. They represent snapshots in time (evidence) and are preserved for compliance/reference but are not active documentation.

| Path / Pattern | Purpose |
| :--- | :--- |
| `HOSTILE_AUDIT_REPORT.md` / `docs/hostile-audit/*.md` | Historical validation reports proving robustness under chaos. |
| `docs/independence/*.md` (excluding runbooks/messaging/databases) | Operational independence assessments from onboarding sprints. |
| `docs/certification/*.md` | Phase-by-phase feature and security certifications. |
| `docs/reports/*.md` | Sprint closure reports and domain purity checks. |
| `docs/executive_strategy_audit/*.md` | Risk reports covering intelligence capability and structure. |
| `docs/executive_trust_audit/*.md` | Risk reports covering semantic and explainability trust. |
| `docs/intelligence_governance_audit/*.md` | Audits of predictions integrity and analytical architecture. |
| `docs/recruitment_audit/*.md` | Audits of privacy risk and candidate data map. |
| `docs/workforce_audit/*.md` | Risk evaluations covering fraud, compliance, and complexity. |
| `docs/capability_audit/*.md` | Audits of talent discovery and skills growth. |
| `docs/institutionalization/KNOWLEDGE_GAP_REPORT.md` | Evaluation of missing documentation and architectural debt. |
| `docs/governance/final_sustainability_report.md` | Historical compliance and sustainability assessment. |
| `docs/governance/final_sustainability_certification.md` | Certification checklists for environmental correctness. |
| `docs/governance/platform_simplification_report.md` | Re-alignment sprint metric findings. |
| `docs/governance/metrics/complexity_dashboard.md` | Code complexity metrics. |
| `PLATFORM_SURVIVABILITY_REPORT.md` | Snapshot report on infrastructure durability. |
| `SESSION_MISTAKES_REFERENCE.md` | Developer reference of past issues (to be kept for learning). |

---

## Category C: Obsolete
These documents contain redundant information, duplicate instructions, or out-of-date sprint notes. They will be merged into Category A files and deleted.

| Path | Duplicate/Redundant With | Action |
| :--- | :--- | :--- |
| `README-LOCAL.md` | `docs/development/local-setup/README.md` | **Merge and Delete** |
| `docs/engineering/local_setup.md` | `docs/development/local-setup/README.md` | **Merge and Delete** |
| `docs/database-contexts.md` | `docs/independence/database-registry.md` | **Delete** (Registry is the source of truth) |
| `docs/health-checks.md` | `docs/closure/health-endpoints.md` | **Delete** (Kubernetes health check has active specs) |
| `docs/sql-resiliency.md` | `docs/governance/reliability_standards.md` | **Delete** |
| `docs/outbox-audit.md` | `docs/independence/messaging-catalog.md` | **Delete** |
| `docs/architecture-hardening-report.md`| None (superseded by final hardening report) | **Delete** |
| `docs/architecture-validation.md` | `docs/certification/architecture.md` | **Delete** |
| `docs/observability.md` | `docs/independence/operations-observability.md`| **Delete** |
| `docs/refactoring-forecasting-billing.md`| None (old sprint log) | **Delete** |
| `docs/refactoring-payroll-notifications.md`| None (old sprint log) | **Delete** |
