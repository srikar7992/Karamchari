# D1 Implementation Report: Execution Context Preservation System

## Overview
The D1 incident exposed a systemic failure in metadata propagation across asynchronous boundaries. The remediation transformed a missing `TenantId` fix into a foundational "Execution Context Preservation System" with cryptographic integrity.

## Key Changes

### 1. Infrastructure-Level Metadata Contract
Established `ExecutionContextHeaders` as the authoritative contract for all asynchronous metadata. Split metadata into three tiers:
- **Tier 1 (Isolation)**: `TenantId`, `ExecutionContextSignature`.
- **Tier 2 (Audit)**: `CorrelationId`, `ConversationId`, `SourceService`, `Version`.
- **Tier 3 (Telemetry)**: `TraceId`.

### 2. Generic Filter Abstraction
Discovered that MassTransit 8.3.0 requires **generic filters** (`IFilter<PublishContext<T>>`) to be correctly applied to the `ScopedPublishEndpoint` used by the Entity Framework Outbox. 
- Refactored `TenantPublishFilter<T>` and `TenantSendFilter<T>` to be generic.
- Added a non-generic `TenantPublishFilter` to cover edge cases like `Fault<T>` messages.

### 3. Cryptographic Integrity (HMAC-SHA256)
Implemented `ExecutionContextSigner` to sign all Tier 1 and Tier 2 metadata.
- **Sign on Publish**: Core infrastructure signs a canonicalized string of metadata before Outbox capture.
- **Validate on Consume**: `TenantConsumeFilter` validates the signature before allowing any business processing.
- **Trust Model**: Headers are only trusted if produced and signed by platform infrastructure.

### 4. Chain of Custody Fix
The fix ensures that metadata is injected **synchronously** during the `Publish()` call in the originating request scope.
1. `EmployeeService.Publish()` called.
2. `TenantPublishFilter<T>.Send()` executes in request scope.
3. Metadata and Signature are added to `context.Headers`.
4. MassTransit `ScopedPublishEndpoint` captures message + headers.
5. `OutboxMessage` row is persisted with JSON headers.
6. Worker consumes, `TenantConsumeFilter` validates and establishes ambient context.

## Implementation Details
- **Core project**: Added `ExecutionContextHeaders.cs`, `ExecutionContextSigner.cs`, and updated filters.
- **API project**: Updated `MassTransitExtensions.cs` to use generic filters and enabled `UseBusOutbox()` for transactional safety.
- **Worker project**: Updated registrations to match generic filter signatures.

## Rollout Strategy
The fix is now ready for phased rollout:
- **Local/PR**: Verified via automated regression suite.
- **Staging**: Next step. Manual verification of Outbox and RabbitMQ messages.
- **Production**: Canary rollout (1% -> 10% -> 50% -> 100%).
