# Execution Context Preservation: Engineer Walkthrough

## Objective
This document enables any engineer to understand, trace, and diagnose the Execution Context Preservation system without prior context.

## 1. The Core "Why"
**The Problem**: MassTransit Entity Framework Outbox captures messages *before* transport-level filters run. This caused `TenantId` loss in D1.
**The Solution**: Generic filters that run synchronously during the `Publish()` call in the request scope. Metadata is signed via HMAC to prevent tampering.

## 2. Architecture Components
- **ExecutionContextHeaders**: Defines the `MT-` header keys.
- **ExecutionContextSigner**: Handles HMAC-SHA256 signing and dual-key validation.
- **TenantPublishFilter<T>**: Generic filter that stamps and signs headers pre-outbox.
- **TenantConsumeFilter<T>**: Generic filter that validates signatures and establishes context post-transport.

## 3. Tracing a Message
To trace a message end-to-end:
1.  **API Log**: `Tenant headers signed and injected for message type {MessageType}`.
2.  **Database**: Query `OutboxMessages`. The `Headers` JSON column must contain `MT-Tenant-Id` and `MT-Context-Signature`.
3.  **Worker Log**: `Signature validated for Tenant {TenantId}`.

## 4. Diagnosing Failures

### Symptom: "Missing TenantId" in Consumer
- **Cause**: The `TenantPublishFilter` did not run on the producer side.
- **Fix**: Ensure the filter is registered as a generic filter `cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context)`.

### Symptom: "Invalid Signature"
- **Cause**: Header tampering, manual injection, or key mismatch during rotation.
- **Fix**: Check `ExecutionContextSigner` configuration. Verify that the producer and consumer share the same `SigningSecret`.

### Symptom: "Stale Message"
- **Cause**: Message arrived after the TTL (default 5 mins) or was replayed from a very old backup.
- **Fix**: Check `MT-Timestamp` header.

## 5. Adding a New Event
When adding a new integration event, you **must**:
1.  Define the record in `Karamchari.Core.Contracts`.
2.  Add a test case to `TenantPropagationRegressionTests` or similar to prove it carries headers.
3.  **DO NOT** manually set `MT-` headers in your domain code. The infrastructure handles it.
