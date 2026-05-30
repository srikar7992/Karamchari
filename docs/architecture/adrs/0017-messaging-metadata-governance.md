# ADR-0017 — Messaging Metadata Governance

**Status:** Accepted  
**Date:** 2026-05-30  
**Author:** Gemini CLI

---

## Context

The D1 incident was caused by a disconnect between infrastructure and domain code regarding metadata ownership. For the multi-tenant isolation model to be robust, the infrastructure must have authoritative ownership over the execution context. If domain developers can manually override `TenantId` or other headers, the isolation guarantees and cryptographic signatures can be bypassed or broken.

---

## Decision

### Authoritative Infrastructure Ownership

1.  **Immutability**: Execution context metadata (`TenantId`, `CorrelationId`, `ConversationId`, `SourceService`) is strictly immutable after the initial `Publish()` or `Send()` call is captured by the infrastructure.
2.  **Infrastructure Stamping**: Only the core messaging infrastructure (via the `TenantPublishFilter` and `TenantSendFilter`) is permitted to stamp these headers.
3.  **Prohibition of Domain Overrides**: Domain services and application handlers are strictly forbidden from manually setting or overriding any header prefixed with `MT-` (defined in `ExecutionContextHeaders`).

### Governance & Enforcement

1.  **Architecture Tests**: Compile-time architecture tests (NetArchTest) will enforce that:
    - Domain assemblies do not reference `Karamchari.Core.Messaging.Tenant` internals.
    - No assembly outside of `Karamchari.Core` is permitted to set `ExecutionContextHeaders` constants in a MassTransit context.
2.  **Validation Rule**: The `TenantConsumeFilter` will treat any mismatch between the business payload (e.g., `event.TenantId`) and the infrastructure header (`MT-Tenant-Id`) as a critical isolation failure and **REJECT** the message.
3.  **New Event Checklist**: Every new integration event must include an automated propagation test to prove it respects the Execution Context Preservation System.

---

## Consequences

- **Hardened Isolation**: Prevents accidental or malicious "tenant jumping" by developers.
- **Forensic Integrity**: Ensures that the `CorrelationId` and `SourceService` headers accurately reflect the actual execution path.
- **Developer Friction**: Domain developers cannot use "quick hacks" involving manual header manipulation; they must rely on the established infrastructure.
- **Platform Homogeneity**: All integration events across all bounded contexts will carry identical, signed metadata.
