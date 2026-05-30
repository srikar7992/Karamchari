# ADR-0015 — Execution Context Preservation

**Status:** Accepted  
**Date:** 2026-05-30  
**Author:** Gemini CLI

---

## Context

The D1 incident exposed a failure in the preservation of execution context (specifically `TenantId`) across the asynchronous boundary formed by the MassTransit Entity Framework Outbox. Integration events published from the API arrived at the Worker with missing headers, leading to database writes landing in the `dbo` schema instead of tenant-specific schemas.

Investigation revealed that transport-level filters (registered inside `UsingRabbitMq` or `UsingInMemory`) execute only during message delivery, which occurs *after* the EF Outbox has captured and serialized the message. Consequently, headers injected by these filters were never persisted in the Outbox.

---

## Decision

### Infrastructure-Centric Metadata Injection

We will enforce a system where execution context metadata is injected synchronously during the `IPublishEndpoint.Publish` call, within the scope of the originating request. This ensures that the metadata is present when the Scoped Outbox captures the message.

**Technical Strategy:**
1.  **Generic Filters**: Use `IFilter<PublishContext<T>>` and `IFilter<SendContext<T>>`. MassTransit 8.3 requires generic filters for correct application to the Scoped Publish Endpoint.
2.  **Global Registration**: Register filters globally using the `x.UsePublishFilter(typeof(TenantPublishFilter<>), context)` pattern outside of transport-specific configuration blocks.
3.  **Tiered Metadata Contract**: Define `ExecutionContextHeaders` to categorize metadata by criticality (Isolation, Audit, Telemetry).

### Cryptographic Integrity (HMAC-SHA256)

To prevent header tampering, manual injection, or corruption during Outbox replay, every message will carry an `MT-Context-Signature`.

- **Generator**: HMAC-SHA256 using a platform-wide secret.
- **Input**: A canonicalized string of Tier 1 (Isolation) and Tier 2 (Audit) metadata.
- **Enforcement**: The `TenantConsumeFilter` will validate this signature before establishing the `TenantExecutionContext`. Messages with invalid or missing signatures will be **REJECTED** and routed to the Dead Letter Queue (DLQ).

### Outbox Capture Boundary

The solution strictly honors the Outbox capture boundary:
- **Pre-Capture**: Metadata injection and signing occur in the API request scope.
- **At Capture**: MassTransit's Scoped Endpoint captures the message + signed headers into the `OutboxMessage` table.
- **Post-Capture**: Transport-level delivery occurs; no further metadata mutation is permitted.

---

## Consequences

- **Guaranteed Isolation**: `TenantId` is persisted in the Outbox, ensuring workers always resolve the correct schema.
- **Tamper Resistance**: Distributed components cannot manually spoof tenant context without the platform secret.
- **Replay Survivability**: Metadata is immutable once signed, surviving Outbox replays and DLQ movements.
- **Operational Complexity**: Adds a requirement for cryptographic key management (addressed in ADR-0016).
- **Performance**: Negligible overhead (< 1ms) for HMAC generation per message.
