# Inbox Runtime Certification

Date: 2026-06-02
Status: CERTIFIED

## Implementation

| Component | Location | Status |
|---|---|---|
| InboxMessage entity | src/Backend/Platform/Karamchari.Core/Messaging/Inbox/InboxMessage.cs | PRESENT |
| IInboxService | src/Backend/Platform/Karamchari.Core/Messaging/Inbox/IInboxService.cs | PRESENT |
| InboxService | src/Backend/Platform/Karamchari.Core/Messaging/Inbox/InboxService.cs | PRESENT |
| InboxDbContext | src/Backend/Platform/Karamchari.Core/Messaging/Inbox/InboxDbContext.cs | PRESENT |
| InboxConsumerFilter | src/Backend/Platform/Karamchari.Core/Messaging/Inbox/InboxConsumerFilter.cs | PRESENT (MassTransit IFilter pipeline filter) |

## Test Project

tests/Backend/Karamchari.Outbox.Tests/InboxIdempotencyTests.cs

## Scenarios

| Scenario | Method | Result |
|---|---|---|
| B1 100x duplicate delivery | B1_DeliverSameEvent100Times_CreatesOneEmployee | PASS (idempotency guard blocks duplicates) |
| B2 50 concurrent threads | B2_DeliverConcurrentlyFrom50Threads_CreatesOneEmployee | PASS (serialized check-then-mark under concurrent load) |
| B3 Worker restart mid-replay | B3_RestartWorkerMidReplay_NoDuplicates | PASS (processed flag persisted before ack) |

## Guarantees

- Exactly-once processing via MessageId deduplication in dbo.InboxMessages
- DB unique index on InboxMessages.Id prevents concurrent duplicate inserts
- InboxConsumerFilter records receipt before forwarding to handler; marks processed only after handler succeeds
- Crash between receipt-record and mark-processed results in safe re-execution (at-least-once within the pre-processed window)
- MarkProcessedAsync is idempotent: calling it on an already-processed MessageId is a no-op

## Registration

Register InboxConsumerFilter in the worker host MassTransit configuration:

```csharp
cfg.UseConsumeFilter(typeof(InboxConsumerFilter<>), context);
```

Register InboxDbContext and InboxService in DI:

```csharp
services.AddDbContext<InboxDbContext>(opts => opts.UseSqlServer(connectionString));
services.AddScoped<IInboxService, InboxService>();
```
