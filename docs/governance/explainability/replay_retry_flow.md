# Replay & Retry Flow

```mermaid
graph TD
    A[Message Received] --> B{Is Duplicate?}
    B -- Yes --> C[Discard Message]
    B -- No --> D[TryRecord in Redis]
    D --> E[Establish Tenant Scope]
    E --> F[Execute Consumer]
    F -- Success --> G[Acknowledge]
    F -- Failure --> H{Retry Count < Limit?}
    H -- Yes --> I[Wait & Retry]
    H -- No --> J[Move to Dead-Letter Queue]
    I --> K[Clear SQL Session Context]
    K --> E
```

## How to Debug
1.  **Duplicate Detection**: Check Redis for key `replay:{MessageId}`. If present, message is discarded.
2.  **Retry Storm**: Check Seq for `tenant.retry_attempt > 0`. If high, investigate downstream service latency or DB deadlocks.
3.  **Context Preservation**: Ensure the `TenantExecutionEnvelope` is identical across retries.
