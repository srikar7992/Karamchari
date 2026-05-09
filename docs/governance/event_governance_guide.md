# Event Governance Guide

## 1. Event Structure Standard
All outbound events MUST conform to the `EnterpriseEventEnvelope` structure. No naked entity publishing.

```json
{
  "eventId": "guid",
  "eventType": "ReimbursementSubmitted",
  "eventVersion": "1.0",
  "occurredAtUtc": "2026-05-09T00:00:00Z",
  "tenantId": "tenant_123",
  "correlationId": "trace_456",
  "causationId": "cmd_789",
  "producer": "karamchari.payroll",
  "payload": { ... }
}
```

## 2. Versioning Rules
- **Additive Evolution Only:** You may add fields to `payload`.
- **Breaking Changes:** If removing or renaming a field, create a new `eventType` (e.g., `ReimbursementSubmittedV2`) and publish BOTH during the deprecation window (60 days).
- **No Silent Breaks:** Modifying an existing event structure without versioning is a P0 architecture violation.

## 3. Idempotency & Retries
- Consumers MUST use `eventId` as the idempotency key.
- Events must be designed to be processed safely multiple times.
- Outbox publishing is mandatory for all domain events to guarantee at-least-once delivery.

## 4. Tenant Propagation
- `tenantId` is a top-level mandatory field in the envelope.
- Consumers MUST establish tenant context using this field before processing the `payload`.
