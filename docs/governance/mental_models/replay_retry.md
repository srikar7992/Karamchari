# Mental Model: Replay & Retry Flow

## Purpose
To guarantee that transient failures and duplicate message deliveries never compromise tenant isolation or result in double-processing.

## The Core Concept
*   **Retry**: An operation failed and is being attempted again. It increments the `RetryAttempt` counter but uses the *exact same* Execution Envelope.
*   **Replay**: An identical message was delivered twice by the broker (or intentionally replayed). It is intercepted by the `ReplayProtectionService`.

## The Lifecycle

### 1. Retry Flow
1.  Message consumption fails due to a SQL deadlock.
2.  MassTransit's retry policy triggers.
3.  Before execution, `RetrySafeSessionReset` wipes the pooled SQL connection clean.
4.  `TenantConsumeFilter` extracts the envelope, increments `RetryAttempt`, and establishes a fresh `TenantExecutionContext`.
5.  The business logic executes with a clean slate.

### 2. Replay Flow
1.  Message arrives at the `TenantConsumeFilter`.
2.  The filter extracts `MessageId` and `TenantId`.
3.  It calls `ReplayProtectionService.TryRecord()`.
4.  This service uses an atomic Redis lock (`SETNX`) keyed by the message ID and content hash.
5.  If the lock already exists, the message is immediately discarded as a duplicate.

## Debugging Strategy
*   **Is it a retry storm?** Check the `RetryAttempt` field in Seq. If it reaches the maximum limit, the message will move to the dead-letter queue.
*   **Was it processed twice?** Search Seq for the `MessageId`. You should see one successful processing log and subsequent "Duplicate message discarded" warnings.

## Invariants
*   State is NEVER carried over between retries.
*   Duplicate messages NEVER reach business logic.
