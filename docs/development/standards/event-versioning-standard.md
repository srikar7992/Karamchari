# Event Versioning Standard

**Status:** Active
**Date:** 2026-06-21
**Driver:** Certification finding **H-1** (`CERTIFICATION.md`) — no explicit event-schema versioning / compatibility gate. Contract-type renames or moves break URN resolution in `OutboxMessageTypeRegistry` and poison-dead-letter the message.
**Related:** [ADR 0017 — Messaging Metadata Governance](../../architecture/adrs/0017-messaging-metadata-governance.md), `Karamchari.Core/Messaging/EnterpriseEventEnvelope.cs`, `Karamchari.Core/Messaging/IIntegrationEvent.cs`

---

## 1. What already exists

`EnterpriseEventEnvelope<TPayload>` already carries the mandatory metadata:

`EventId`, `EventType`, `EventVersion` (default `"1.0"`), `OccurredAtUtc`, `TenantId`, `CorrelationId`, `CausationId`, `Producer`, `Payload`.

This standard does **not** add new fields. It governs **how payload contracts evolve** so producers and consumers stay compatible across deploys.

## 2. Core rules (non-negotiable)

1. **Contracts are immutable once shipped.** A published integration-event record (e.g. `EmployeeCreatedV1`) is **never** mutated — no field added, removed, renamed, retyped, or reordered after it has been produced in any non-Development environment.
2. **Breaking change ⇒ new versioned type.** A change that removes/renames/retypes a field, or changes its meaning, creates a new record with an incremented version suffix:
   ```csharp
   public sealed record EmployeeCreatedV1(Guid EmployeeId, string FullName) : IIntegrationEvent;

   // Later — breaking change (FullName split). NEW type, V1 stays untouched.
   public sealed record EmployeeCreatedV2(Guid EmployeeId, string FirstName, string LastName) : IIntegrationEvent;
   ```
3. **Additive change is allowed in place only if backward-compatible.** Adding an **optional** field (nullable, or with a safe default) to an unshipped or JSON-tolerant contract is permitted because old consumers ignore unknown JSON and new consumers tolerate the missing value. When in doubt, version.
4. **`EventVersion` is set explicitly per contract**, matching the type suffix: `EmployeeCreatedV2` ⇒ `version: "2.0"` via `EnterpriseEventEnvelope.Wrap(..., version: "2.0")`.
5. **Producers may publish V1 and V2 in parallel** during a migration window. A producer upgrades to emit V2 only after all consumers understand V2 (or an upcaster exists).
6. **Type identity is the URN.** Because `OutboxMessageTypeRegistry` resolves the CLR type from the message-type URN, a contract type's namespace + name is part of its wire contract. **Do not move or rename a shipped contract type.** If a type must relocate, keep a shim type at the old URN.

## 3. Consumer evolution

- **New consumer reading old events:** support the lowest version still in flight. Keep `Consume(EmployeeCreatedV1)` until V1 is confirmed drained from outbox + broker + DLQ.
- **Old consumer reading new events:** old consumers continue to receive V1 until producers cut over; they never see V2 until they declare a `Consume(EmployeeCreatedV2)`.
- **Upcasters** bridge versions when a single consumer should handle both: translate V1 → V2 at the edge so business logic handles only the latest shape.
  ```csharp
  public static EmployeeCreatedV2 Upcast(EmployeeCreatedV1 v1)
  {
      var (first, last) = SplitName(v1.FullName);
      return new EmployeeCreatedV2(v1.EmployeeId, first, last);
  }
  ```

## 4. Deprecation lifecycle

`Active` → `Deprecated` (announced; producers stop emitting; consumers keep reading) → `Retired` (no longer in flight anywhere; `Consume` handler + type may be deleted). Record each transition in the contract's XML doc comment and in this module's changelog.

## 5. Enforcement gaps to close (tracked under H-1)

1. **`IIntegrationEvent` is an empty marker.** Recommendation: require every integration event to be published through `EnterpriseEventEnvelope` (assert in an architecture test that public `IIntegrationEvent` records are produced only via `Wrap`).
2. **No CI compatibility gate.** Add a contract-snapshot check:
   - Generate a JSON-schema (or serialized shape) snapshot per `IIntegrationEvent` type into `tests/contracts/snapshots/`.
   - CI fails if a **shipped** snapshot changes (mutation of a frozen contract) — additions of new `*V2` types are allowed.
   - Wire as a job in `ci.yml` after the build step.
3. **Architecture test** (add to `Karamchari.ArchitectureTests`): every `IIntegrationEvent` implementer's type name matches `^[A-Za-z]+V\d+$` (forces explicit versioning suffix), and lives in a `*.Contracts` project (URN stability).

## 6. Checklist for any event change (PR reviewers enforce)

- [ ] Did I mutate a shipped contract? → **Reject.** Create `...V{n+1}` instead.
- [ ] New type ends in `V{n}` and sets matching `EventVersion`?
- [ ] Type lives in a `*.Contracts` project; namespace/name unchanged for existing types?
- [ ] Consumers for all in-flight versions present (or upcaster added)?
- [ ] Contract snapshot updated only by *addition*, never by edit of a frozen file?
