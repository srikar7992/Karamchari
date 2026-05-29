# Karamchari Platform Concept Budget

**Purpose**: To actively prevent abstraction sprawl and ensure runtime concepts remain manageable for a single human to hold in their head.

## The Budget Rule
The platform is granted a strict budget of **15 Core Runtime Primitives**. 
Currently, we consume **11**. 

If a new primitive is proposed that breaches the budget, an existing primitive MUST be deprecated and removed.

## Current Registry (11/15)
1.  `TenantExecutionEnvelope` (Identity & Propagation)
2.  `TenantExecutionContext` (AsyncLocal Scope Management)
3.  `TenantConsumeFilter` (Messaging Entry)
4.  `TenantJobExecutionScope` (Background Entry)
5.  `TenantSqlConnectionScope` (DB Execution Boundary)
6.  `RlsConnectionGuard` (DB Session Contamination Guard)
7.  `ReplayProtectionService` (Idempotency)
8.  `TenantQueryValidationInterceptor` (SQL Injection/Schema Protection)
9.  `TenantCacheGuard` (Cache Isolation)
10. `TenantActivitySource` (Distributed Tracing)
11. `TenantProvisioningService` (Tenant Lifecycle)

## Abstraction Proposal Checklist
Any PR introducing a new runtime concept must answer:
1.  Why are the existing 11 primitives insufficient?
2.  What operational or debugging problem does this solve that justifies the cognitive load?
3.  If this pushes the budget over 15, which concept is being proposed for deletion?

## Complexity Trend Monitoring
Every quarter, the Enterprise Runtime Governance Lead reviews the usage of these 11 concepts. Concepts that are highly coupled, confusing to new hires, or difficult to trace in Seq will be targeted for unification or deletion.