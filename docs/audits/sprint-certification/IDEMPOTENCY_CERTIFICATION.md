# Idempotency Certification
**Date:** 2026-06-01  
**Status:** CONDITIONALLY CERTIFIED

---

## HTTP-Level Idempotency (X-Idempotency-Key)

| Component | Status |
|---|---|
| IdempotentRequest domain class | IMPLEMENTED |
| CoreDbContext.IdempotentRequests DbSet | IMPLEMENTED |
| EF migration (dbo.IdempotentRequests) | IMPLEMENTED (20260601000000_AddCoreIdempotencyAndAudit.cs) |
| IdempotencyFilter (IEndpointFilter) | IMPLEMENTED — X-Idempotency-Key header check |
| Response caching (24h TTL) | IMPLEMENTED |
| Expired record cleanup | IMPLEMENTED — IdempotencyCleanupWorker (12h interval, batch 1000) |
| IdempotencyExtensions.WithIdempotency() | IMPLEMENTED |

## Domain-Level Idempotency

| Scenario | Implementation | Tests |
|---|---|---|
| Offer accept twice | InvalidOperationException in domain | OfferAcceptShouldBeIdempotent |
| Hire twice | InvalidOperationException in domain | HireCandidateShouldBeIdempotent |
| Billing event dedup | ProcessedEventLog (EventId + ConsumerName PK) | BillingDbContext |
| Notification dedup | IdempotencyKey = TenantId:TriggerEventId:Category:RecipientId | NotificationMessageTests |
| Compensation revision dedup | ExternalRevisionId check in ApplyRevision() | EmployeeCompensationRecordTests |
| Learning enrollment completion | CompletionIdempotencyKey check | LearningEnrollmentTests |
| Outbox inbox dedup | InboxState (MassTransit built-in) | Architecture |

## Runtime Blockers

- HTTP-level idempotency E2E test (requires running API + DB)
- Concurrent duplicate request test (50 simultaneous requests)
- TTL expiration cleanup verification (requires time-advance)

## Certification Decision

**CONDITIONALLY CERTIFIED** — implementation complete at all layers. Runtime concurrent stress test needs live infrastructure.
