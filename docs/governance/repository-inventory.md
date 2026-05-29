# Repository Documentation Inventory

This document lists all documentation files currently in the repository, their purposes, last modified dates, and references.

| Path | Purpose | Last Modified | Referenced by MD | Referenced by Scripts | Referenced by CI/CD | Referenced in README |
| :--- | :--- | :---: | :--- | :--- | :--- | :---: |
| .github/pull_request_template.md | Validation | 2026-05-09 | None | None | None | No |
| BUS_FACTOR_REPORT.md | Bus Factor Report | 2026-05-30 | None | None | None | No |
| DEPLOYMENT_CERTIFICATION_REPORT.md | Deployment Certification Report | 2026-05-30 | None | None | None | No |
| DOMAIN_KNOWLEDGE_REPORT.md | Domain Knowledge Report | 2026-05-30 | None | None | None | No |
| HOSTILE_AUDIT_REPORT.md | Karamchari Hostile Audit Report | 2026-05-30 | None | None | None | No |
| INDEPENDENCE_CERTIFICATION_REPORT.md | Independence Certification Report | 2026-05-30 | None | None | None | No |
| OBSERVABILITY_CERTIFICATION_REPORT.md | Observability Certification Report | 2026-05-30 | None | None | None | No |
| PLATFORM_INDEPENDENCE_REPORT.md | Karamchari Platform Operational Independence & Handoff Audit Report | 2026-05-30 | None | None | None | No |
| PLATFORM_SURVIVABILITY_REPORT.md | Platform Survivability Report | 2026-05-30 | None | None | None | No |
| README-LOCAL.md | Karamchari Local Developer Quick Start | 2026-05-29 | HOSTILE_AUDIT_REPORT.md, documentation-audit.md, runbooks.md, new-developer-onboarding.md | None | None | No |
| README.md | Karamchari | 2026-05-10 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | No |
| RUNBOOK_COVERAGE_REPORT.md | Runbook Coverage Report | 2026-05-30 | None | None | None | No |
| SESSION_MISTAKES_REFERENCE.md | Session Mistakes & Fixes Reference | 2026-05-12 | None | None | None | No |
| TEST_COVERAGE_REPORT.md | Test Coverage Report | 2026-05-30 | None | None | None | No |
| docs/adr/0001-multi-tenancy-model.md | ADR 0001 — Multi-tenancy: shared database, separated schema, single EF model | 2026-05-03 | KNOWLEDGE_GAP_REPORT.md, README.md | None | None | Yes |
| docs/adr/0002-row-level-security.md | ADR 0002 — Row-Level Security as the database-layer failsafe | 2026-05-03 | README.md | None | None | No |
| docs/adr/0003-rls-script-generation-workflow.md | ADR 0003 — RLS script generation workflow | 2026-05-03 | README.md | None | None | No |
| docs/adr/0004-performance-bc-architecture.md | ADR 0004 — Performance BC Architecture | 2026-05-08 | README.md | None | None | No |
| docs/adr/0005-anonymous-feedback-hmac-privacy.md | ADR 0005 — Anonymous Feedback HMAC Privacy Model | 2026-05-08 | README.md | None | None | No |
| docs/adr/0006-kpi-formula-engine.md | ADR 0006 — KPI Formula Engine: Temporary Roslyn → Planned AST DSL | 2026-05-08 | README.md | None | None | No |
| docs/adr/0007-performance-dbcontext-split-roadmap.md | ADR 0007 — PerformanceDbContext Split Roadmap | 2026-05-08 | README.md | None | None | No |
| docs/adr/0008-notification-orchestration-platform.md | ADR 0008 — Notification Orchestration Platform | 2026-05-08 | README.md | None | None | No |
| docs/adr/0009-cqrs-read-model-strategy.md | ADR 0009 — CQRS Read-Model Strategy | 2026-05-08 | README.md | None | None | No |
| docs/adr/0010-search-talent-discovery-architecture.md | ADR 0010 — Search & Talent Discovery Architecture | 2026-05-08 | README.md | None | None | No |
| docs/adr/0011-bff-architecture.md | ADR-0011: BFF Architecture — Persona Route Groups within Modular Monolith | 2026-05-08 | README.md | None | None | No |
| docs/adr/0012-notification-orchestration-pipeline.md | ADR-0012 — Notification Orchestration Pipeline | 2026-05-08 | README.md | None | None | No |
| docs/adr/0013-reporting-export-pipeline.md | ADR-0013 — Reporting & Export Pipeline | 2026-05-09 | README.md | None | None | No |
| docs/adr/0014-search-infrastructure.md | ADR-0014 — Search Infrastructure | 2026-05-09 | KNOWLEDGE_GAP_REPORT.md, README.md | None | None | No |
| docs/api-explorer.md | API Explorer Configuration | 2026-05-29 | None | None | None | No |
| docs/architecture-hardening-report.md | Karamchari Architecture Hardening Sprint Report | 2026-05-29 | None | None | None | No |
| docs/architecture-validation.md | Architecture Validation Report | 2026-05-29 | None | None | None | No |
| docs/architecture/adrs/README.md | Architecture ADR Preservation Audit | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/capability_audit/learning_scalability_report.md | Learning Scalability Report | 2026-05-09 | None | None | None | No |
| docs/capability_audit/readiness_intelligence_dependency_map.md | Readiness Intelligence Dependency Map | 2026-05-09 | None | None | None | No |
| docs/capability_audit/skill_governance_report.md | Skill Governance Report | 2026-05-09 | None | None | None | No |
| docs/capability_audit/workforce_capability_risk_report.md | Workforce Capability Risk Report | 2026-05-09 | None | None | None | No |
| docs/certification/architecture.md | Phase 17 — Architecture Compliance Re-Validation | 2026-05-29 | README.md, final-certification-report.md | None | None | No |
| docs/certification/authentication.md | Phase 4 — Authentication Certification | 2026-05-29 | final-certification-report.md, scalar.md | None | None | No |
| docs/certification/business-workflows.md | Phase 18 — Business Workflow Certification | 2026-05-29 | final-certification-report.md | None | None | No |
| docs/certification/chaos.md | Phase 16 — Chaos Certification | 2026-05-29 | environment.md, final-certification-report.md | None | None | No |
| docs/certification/correlation.md | Phase 13 — Correlation Certification | 2026-05-29 | final-production-readiness-report.md, final-certification-report.md | None | None | No |
| docs/certification/database.md | Phase 10 — Database Certification | 2026-05-29 | final-certification-report.md | None | None | No |
| docs/certification/employee.md | Phase 5 — Employee Domain Certification | 2026-05-29 | final-certification-report.md | None | None | No |
| docs/certification/environment.md | Phase 1 — Environment Certification | 2026-05-29 | final-certification-report.md | None | None | No |
| docs/certification/final-certification-report.md | Karamchari Platform — Final Certification Report | 2026-05-29 | None | None | None | No |
| docs/certification/logging.md | Phase 12 — Logging Certification | 2026-05-29 | final-certification-report.md | None | None | No |
| docs/certification/opentelemetry.md | Phase 11 — OpenTelemetry Certification | 2026-05-29 | startup.md, final-certification-report.md | None | None | No |
| docs/certification/outbox.md | Phase 9 — Outbox Certification | 2026-05-29 | rabbitmq.md, final-certification-report.md | None | None | No |
| docs/certification/performance.md | Phase 15 — Performance Smoke Certification | 2026-05-29 | final-certification-report.md | None | None | No |
| docs/certification/rabbitmq.md | Phase 8 — RabbitMQ Certification | 2026-05-29 | outbox.md, startup.md, final-certification-report.md | None | None | No |
| docs/certification/redis.md | Phase 7 — Redis Certification | 2026-05-29 | final-certification-report.md, tenant-isolation.md | None | None | No |
| docs/certification/scalar.md | Phase 3 — Scalar / OpenAPI Certification | 2026-05-29 | final-certification-report.md | None | None | No |
| docs/certification/security.md | Phase 14 — Security Certification | 2026-05-29 | README.md, authentication.md, final-certification-report.md | None | None | No |
| docs/certification/startup.md | Phase 2 — Application Startup Certification | 2026-05-29 | final-certification-report.md, database.md | None | None | No |
| docs/certification/tenant-isolation.md | Phase 6 — Multi-Tenant Isolation Certification | 2026-05-29 | rabbitmq.md, employee.md, final-certification-report.md | None | None | No |
| docs/closure/architecture-governance.md | WS10 — Architecture Governance Enforcement | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/authentication-recovery.md | WS1 — Identity & Authentication Recovery | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/business-certification.md | WS15 — Full Business Workflow Certification + Greenfield Bootstrap | 2026-05-29 | authentication-recovery.md, final-production-readiness-report.md | None | None | No |
| docs/closure/correlation.md | WS12 — Correlation Completion | 2026-05-29 | final-production-readiness-report.md, final-certification-report.md | None | None | No |
| docs/closure/decimal-precision.md | WS7 — Financial Data Integrity (Decimal Precision) | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/durability.md | WS13 — Durable Infrastructure | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/error-handling.md | WS6 — Exception & Error Sanitization | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/final-production-readiness-report.md | Karamchari — Final Production Readiness Report | 2026-05-29 | None | None | None | No |
| docs/closure/health-endpoints.md | WS9 — Health & Kubernetes Readiness | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/jwt-hardening.md | WS3 — JWT Security Hardening | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/messaging-isolation.md | WS4 — Messaging Tenant Isolation | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/observability.md | WS11 — Observability Completion | 2026-05-29 | PLATFORM_INDEPENDENCE_REPORT.md, final-production-readiness-report.md, business-certification.md | None | None | No |
| docs/closure/provisioning-idempotency.md | WS2 — Provisioning Idempotency | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/rate-limiting.md | WS14 — Rate Limiting & Abuse Protection | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/referential-integrity.md | WS8 — Referential Integrity | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/closure/security-headers.md | WS5 — Security Headers | 2026-05-29 | final-production-readiness-report.md | None | None | No |
| docs/convergence/01_platform_forensic_analysis.md | Phase X: Platform Forensic Analysis | 2026-05-11 | None | None | None | No |
| docs/convergence/02_canonical_platform_primitives.md | Phase X: Canonical Platform Primitives | 2026-05-11 | None | None | None | No |
| docs/convergence/03_canonical_workforce_domain_model.md | Phase X: Canonical Workforce Domain Model | 2026-05-11 | None | None | None | No |
| docs/convergence/04_unified_workflow_runtime.md | Phase X: Unified Workflow Runtime | 2026-05-11 | None | None | None | No |
| docs/convergence/05_tenant_architecture_consolidation.md | Phase X: Tenant Architecture Consolidation | 2026-05-11 | None | None | None | No |
| docs/convergence/06_simplified_service_topology.md | Phase X: Simplified Service Topology | 2026-05-11 | None | None | None | No |
| docs/convergence/07_operational_admin_experience.md | Phase X: Operational Admin Experience | 2026-05-11 | None | None | None | No |
| docs/convergence/08_convergence_success_criteria.md | Phase X: Convergence Success Criteria | 2026-05-11 | None | None | None | No |
| docs/database-contexts.md | Database Context Registry | 2026-05-29 | None | None | None | No |
| docs/day1-readiness-report.md | Day-1 Readiness Report | 2026-05-29 | None | None | None | No |
| docs/design/stitch/monolith_precision/DESIGN.md | Brand & Style | 2026-05-04 | README.md | None | None | No |
| docs/domain-dictionary/README.md | Enterprise Domain Dictionary | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Billing/README.md | Billing Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Capability/README.md | Capability Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Compensation/README.md | Compensation Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/FinancialOps/README.md | FinancialOps Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Forecasting/README.md | Forecasting Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Governance/README.md | Governance Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/HR/README.md | HR Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Identity/README.md | Identity Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Intelligence/README.md | Intelligence Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Notifications/README.md | Notifications Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/PSA/README.md | PSA Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Payroll/README.md | Payroll Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Performance/README.md | Performance Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Recruitment/README.md | Recruitment Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/TimeAttendance/README.md | TimeAttendance Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/domains/Workflow/README.md | Workflow Domain | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/engineering/analyzer_policy.md | Analyzer & Code Quality Policy | 2026-05-09 | None | None | None | No |
| docs/engineering/branching_strategy.md | Branching Strategy & Protection Rules | 2026-05-09 | None | None | None | No |
| docs/engineering/ci_cd_policy.md | CI/CD Policy | 2026-05-09 | None | None | None | No |
| docs/engineering/commit_conventions.md | Commit Conventions | 2026-05-09 | None | None | None | No |
| docs/engineering/final_validation_report.md | Engineering Hardening Validation Report | 2026-05-09 | None | None | None | No |
| docs/engineering/local_setup.md | Local Setup and Validation | 2026-05-09 | documentation-audit.md | None | None | No |
| docs/engineering/package_governance.md | Centralized Package Governance | 2026-05-09 | None | None | None | No |
| docs/executive_strategy_audit/executive_intelligence_architecture_report.md | Executive Intelligence Architecture Report | 2026-05-09 | None | None | None | No |
| docs/executive_strategy_audit/organizational_strategy_governance_report.md | Organizational Strategy Governance Report | 2026-05-09 | None | None | None | No |
| docs/executive_strategy_audit/strategic_intelligence_trust_report.md | Strategic Intelligence Trust Report | 2026-05-09 | None | None | None | No |
| docs/executive_strategy_audit/workforce_forecasting_readiness_report.md | Workforce Forecasting Readiness Report | 2026-05-09 | None | None | None | No |
| docs/executive_trust_audit/executive_intelligence_trust_report.md | Executive Intelligence Trust Report | 2026-05-09 | None | None | None | No |
| docs/executive_trust_audit/explainability_readiness_report.md | Explainability Readiness Report | 2026-05-09 | None | None | None | No |
| docs/executive_trust_audit/intelligence_semantics_report.md | Intelligence Semantics Report | 2026-05-09 | None | None | None | No |
| docs/executive_trust_audit/organizational_governance_report.md | Organizational Governance Report | 2026-05-09 | None | None | None | No |
| docs/governance/architecture_standards.md | Architecture Standards & Platform Guardrails | 2026-05-09 | None | None | None | No |
| docs/governance/concept_budget.md | Karamchari Platform Concept Budget | 2026-05-10 | None | None | None | No |
| docs/governance/education/onboarding.md | Platform Engineer Onboarding Program | 2026-05-10 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/governance/education/tenant_isolation_lab.md | Tenant Isolation & Idempotency Lab | 2026-05-10 | onboarding.md | None | None | No |
| docs/governance/event_governance_guide.md | Event Governance Guide | 2026-05-09 | None | None | None | No |
| docs/governance/evolution_principles.md | Platform Evolution Principles | 2026-05-10 | None | None | None | No |
| docs/governance/explainability/replay_retry_flow.md | Replay & Retry Flow | 2026-05-10 | None | None | None | No |
| docs/governance/explainability/tenant_execution_flow.md | Tenant Execution Flow | 2026-05-10 | None | None | None | No |
| docs/governance/final_sustainability_certification.md | Final Platform Sustainability Certification | 2026-05-10 | None | None | None | No |
| docs/governance/final_sustainability_report.md | Final Platform Sustainability Report | 2026-05-10 | None | None | None | No |
| docs/governance/golden_paths.md | Karamchari Golden Path Reference Guide | 2026-05-10 | platform_governance_checklist.md | None | None | No |
| docs/governance/mental_models/replay_retry.md | Mental Model: Replay & Retry Flow | 2026-05-10 | None | None | None | No |
| docs/governance/mental_models/tenant_execution.md | Mental Model: Tenant Execution Flow | 2026-05-10 | None | None | None | No |
| docs/governance/metrics/complexity_dashboard.md | Platform Complexity Dashboard | 2026-05-10 | None | None | None | No |
| docs/governance/module_governance.md | Platform Module Governance | 2026-05-12 | final_execution_audit.md | None | None | No |
| docs/governance/platform_contracts.md | Karamchari Platform Contracts | 2026-05-10 | None | None | None | No |
| docs/governance/platform_governance_checklist.md | Platform Governance Checklist | 2026-05-10 | None | None | None | No |
| docs/governance/platform_simplification_report.md | Platform Simplification & Governance Report | 2026-05-10 | None | None | None | No |
| docs/governance/reliability_standards.md | Reliability & Operational Standards | 2026-05-09 | None | None | None | No |
| docs/governance/runbooks/replay_recovery.md | Runbook: Replay & Retry Recovery | 2026-05-10 | operations-audit.md, KNOWLEDGE_GAP_REPORT.md | None | None | No |
| docs/governance/runtime_concepts.md | Runtime Concept Registry | 2026-05-10 | None | None | None | No |
| docs/governance/workflow_governance_guide.md | Workflow Governance Guide | 2026-05-09 | None | None | None | No |
| docs/governance/workforce_temporal_standards.md | Workforce Temporal Standards | 2026-05-09 | None | None | None | No |
| docs/health-checks.md | Health Endpoint Validation Guide | 2026-05-29 | None | None | None | No |
| docs/hostile-audit/cicd-audit.md | CI/CD Audit | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/configuration-audit.md | Production Configuration Audit | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/developer-experience.md | Developer Experience Audit | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/documentation-audit.md | Documentation Truthfulness Audit | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/environment-dependencies.md | Environment Dependency Discovery | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/failure-injection.md | Failure Injection Audit | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/fresh-machine-validation.md | Fresh Machine Validation | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/greenfield-bootstrap.md | Greenfield Bootstrap Re-Certification | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/operations-audit.md | Operational Readiness Audit | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/security-audit.md | Security Skeptic Audit | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/setup-script-certification.md | Setup Script Certification | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/test-quality.md | Test Quality Audit | 2026-05-30 | None | None | None | No |
| docs/hostile-audit/test-runner-certification.md | Test Runner Certification | 2026-05-30 | None | None | None | No |
| docs/independence/architecture-discoverability.md | Phase 2: Architecture Discoverability Audit | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/bus-factor.md | Phase 10: Bus Factor Audit | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/cicd-documentation.md | Phase 6: CI/CD Independence Audit | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/database-registry.md | Phase 7: Database Ownership Audit & Registry | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/domain-documentation.md | Phase 3: Domain Knowledge Audit | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/messaging-catalog.md | Phase 5: Async Workflow Ownership Audit & Messaging Catalog | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/new-developer-onboarding.md | Phase 1: New Developer Time-To-Productivity Audit | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/operations-observability.md | Phase 9: Production Operations Audit & Observability | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/runbooks.md | Phase 4: Operational Runbook Audit | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/independence/test-registry.md | Phase 8: Test Ownership Audit & Registry | 2026-05-30 | PLATFORM_INDEPENDENCE_REPORT.md | None | None | No |
| docs/infrastructure-validation.md | Infrastructure Dependency Validation | 2026-05-29 | None | None | None | No |
| docs/institutionalization/KNOWLEDGE_GAP_REPORT.md | Karamchari Knowledge Gap Report | 2026-05-30 | DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, INDEPENDENCE_CERTIFICATION_REPORT.md | None | None | No |
| docs/intelligence_governance_audit/analytical_architecture_report.md | Analytical Architecture Report | 2026-05-09 | None | None | None | No |
| docs/intelligence_governance_audit/explainability_readiness_report.md | Explainability Readiness Report | 2026-05-09 | None | None | None | No |
| docs/intelligence_governance_audit/scoring_integrity_report.md | Scoring Integrity & Metric Report | 2026-05-09 | None | None | None | No |
| docs/intelligence_governance_audit/workforce_intelligence_governance_report.md | Workforce Intelligence Governance Report | 2026-05-09 | None | None | None | No |
| docs/observability.md | Karamchari Observability Framework | 2026-05-29 | PLATFORM_INDEPENDENCE_REPORT.md, final-production-readiness-report.md, business-certification.md | None | None | No |
| docs/outbox-audit.md | Outbox Coverage Audit | 2026-05-29 | None | None | None | No |
| docs/platform_governance_audit/distributed_maturity_report.md | Distributed Maturity Report | 2026-05-09 | None | None | None | No |
| docs/platform_governance_audit/long_term_scalability_assessment.md | Long-Term Scalability Assessment | 2026-05-09 | None | None | None | No |
| docs/platform_governance_audit/operational_excellence_report.md | Operational Excellence Report | 2026-05-09 | None | None | None | No |
| docs/platform_governance_audit/phase_1g_final_report.md | Phase 1G: Continuous Platform Governance Final Report | 2026-05-09 | None | None | None | No |
| docs/platform_governance_audit/platform_governance_report.md | Platform Governance Report | 2026-05-09 | None | None | None | No |
| docs/recruitment_audit/candidate_privacy_risk_report.md | Candidate Privacy Risk Report | 2026-05-09 | None | None | None | No |
| docs/recruitment_audit/hiring_governance_report.md | Hiring Governance Report | 2026-05-09 | None | None | None | No |
| docs/recruitment_audit/recruitment_operations_risk_report.md | Recruitment Operations Risk Report | 2026-05-09 | None | None | None | No |
| docs/recruitment_audit/workforce_acquisition_dependency_map.md | Workforce Acquisition Dependency Map | 2026-05-09 | None | None | None | No |
| docs/refactoring-forecasting-billing.md | Refactoring Forecasting → Billing Coupling | 2026-05-29 | None | None | None | No |
| docs/refactoring-payroll-notifications.md | Refactoring Payroll → Notifications Dependency | 2026-05-29 | None | None | None | No |
| docs/reports/domain_purity_audit.md | Phase Z: Domain Purity Audit Report | 2026-05-12 | None | None | None | No |
| docs/reports/enterprise_completion_audit_1a_1d.md | Phase 1A–1D Enterprise Completion Audit Report | 2026-05-13 | None | None | None | No |
| docs/reports/enterprise_readiness_audit_1a_1c.md | Phase 1A–1C Completion Verification & Enterprise Readiness Audit Report | 2026-05-12 | None | None | None | No |
| docs/reports/final_execution_audit.md | Karamchari Platform Execution Audit & Validation Report | 2026-05-12 | None | None | None | No |
| docs/reports/operational_scale_hardening_report.md | Post-Remediation Operational Scale Hardening Report | 2026-05-12 | None | None | None | No |
| docs/reports/phase_1c_1_audit.md | Phase 1C.1 Distributed Systems & Governance Audit Report | 2026-05-09 | None | None | None | No |
| docs/reports/remediation_sprint_report.md | Phase 1A–1C Remediation Sprint: Business Truth Hardening Report | 2026-05-12 | None | None | None | No |
| docs/reports/state_ownership_audit.md | Phase Y: Operational Hardening - Audit Report | 2026-05-12 | None | None | None | No |
| docs/reports/workflow_duplication_report.md | Workflow Forensic Analysis: Duplication Report | 2026-05-12 | final_execution_audit.md | None | None | No |
| docs/runbooks/README.md | Operational Runbooks | 2026-05-30 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| docs/sql-resiliency.md | SQL Database Resiliency Report | 2026-05-29 | None | None | None | No |
| docs/startup-sequence.md | Application Startup Sequence | 2026-05-29 | runbooks.md | None | None | No |
| docs/tenant-isolation-certification-report.md | TENANT ISOLATION CERTIFICATION REPORT | 2026-05-10 | None | None | None | No |
| docs/workforce_audit/attendance_fraud_risk.md | Attendance Fraud Risk Report | 2026-05-09 | None | None | None | No |
| docs/workforce_audit/compliance_complexity.md | Compliance Complexity Report | 2026-05-09 | None | None | None | No |
| docs/workforce_audit/workforce_ops_risk.md | Workforce Operations Risk Report | 2026-05-09 | None | None | None | No |
| infrastructure/local/profiles/certification.md | Operating Mode: Certification | 2026-05-10 | authentication-recovery.md, final-production-readiness-report.md | None | None | No |
| infrastructure/local/profiles/lightweight.md | Operating Mode: Lightweight | 2026-05-10 | None | None | None | No |
| src/Frontend/README.md | Karamchari Frontend (Nx workspace) | 2026-05-03 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
| src/Frontend/portal/README.md | Karamchari Portal | 2026-05-04 | RUNBOOK_COVERAGE_REPORT.md, DOMAIN_KNOWLEDGE_REPORT.md, PLATFORM_SURVIVABILITY_REPORT.md, HOSTILE_AUDIT_REPORT.md, PLATFORM_INDEPENDENCE_REPORT.md, developer-experience.md, documentation-audit.md, operations-audit.md, domain-documentation.md, new-developer-onboarding.md | None | None | Yes |
