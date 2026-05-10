# Platform Simplification & Governance Report

**Date**: May 10, 2026
**Status**: ACTIVE

## Executive Summary
The Karamchari platform has successfully achieved enterprise-grade multi-tenant isolation, resilience, and observability. However, this success has introduced an existential risk: **Platform Complexity Collapse**. 

This report provides a brutally honest assessment of the current cognitive load and outlines the strategic consolidation of runtime concepts to ensure the platform remains evolvable for years to come.

## 1. Current Platform Complexity Score
*   **Abstraction Count**: HIGH (>15 core execution interceptors/scopes)
*   **Duplication Risk**: MODERATE (Overlapping tenant scopes across Job, Messaging, and HTTP layers)
*   **Debugging Difficulty**: MODERATE (Telemetry is excellent, but cognitive tracing of scope boundaries is complex)
*   **Overall Sustainability**: YELLOW (Requires immediate simplification to prevent future drag)

## 2. Cognitive Load Assessment
Engineers currently have to understand multiple execution scopes:
- `TenantExecutionContext` (Core)
- `TenantContext` (Core)
- `TenantExecutionScope` (HTTP/General)
- `TenantJobExecutionScope` (Background)
- `TenantMessageConsumerScope` (Messaging)
- `TenantSqlConnectionScope` (Database)

**Verdict**: This is too many concepts. A developer should only need to know "I am in a Tenant Execution Context." The infrastructure must handle the boundary translation transparently.

## 3. Duplicate Abstraction Analysis
*   **`TenantContext` vs `TenantExecutionContext`**: `TenantContext` is a legacy artifact of the initial request-scoped architecture. It overlaps entirely with `TenantExecutionContext.Envelope`. **Action**: Deprecate `TenantContext` entirely.
*   **Scope Duplication**: `TenantJobExecutionScope` implements its own `AsyncLocal` dictionary. This duplicates `TenantExecutionContext._current`. **Action**: Re-route all scopes to use the unified `TenantExecutionContext.Establish()`.

## 4. Platform Layers Restructured
We are officially codifying the platform into four strict layers:

### Layer 1: Runtime Foundation
Core execution semantics ONLY (`TenantExecutionContext`, `ExecutionEnvelope`). No framework dependencies.

### Layer 2: Infrastructure Enforcement
Runtime protection (`RlsConnectionGuard`, `TenantQueryValidationInterceptor`). Depends ONLY on Layer 1.

### Layer 3: Observability & Diagnostics
Passive visibility (`TenantAwareActivityListener`). MUST NOT enforce business behavior.

### Layer 4: Validation & Certification
Isolated runtime verification (`Karamchari.TenantIsolationCertification`). NEVER deployed to production.

## 5. Golden Path Coverage
We have defined canonical Golden Paths for:
- Consumer execution
- Background jobs
- SQL access
- Cache access

Any deviation from these paths is considered a critical architectural violation and will be caught by automated fitness functions.

## 6. Operational Maintainability & Action Plan
To dramatically lower onboarding and debugging complexity, the following actions are mandated:
1.  **Merge Concept**: Eliminate `TenantContext` and replace all references with `TenantExecutionEnvelope`.
2.  **Consolidate Scopes**: Refactor Background and Messaging scopes to be thin wrappers that exclusively delegate state to `TenantExecutionContext.Establish()`.
3.  **Executable Enforcement**: Expand `PlatformFitnessTests` to block raw `DbConnection` usage and catch `IgnoreQueryFilters` via Roslyn Analyzers.
4.  **Isolate Business Logic**: Audit `Karamchari.HR`, `Karamchari.Payroll`, etc., to ensure zero presence of `.Establish()` calls. Domain code must remain completely ignorant of the multi-tenant infrastructure.

## Conclusion
The platform's sophisticated capabilities must be hidden behind a unified, simple mental model. By reducing the concept count and enforcing strict Golden Paths, we guarantee that Karamchari can safely scale both technically and organizationally.
