# D1 Architecture Forensics Report

## Objective
Identify exactly where execution context (TenantId, CorrelationId, etc.) disappears in the asynchronous messaging pipeline.

## Execution Sequence & Evidence

### Hop 1: HTTP Request (API)
- **Status**: ✅ SUCCESS
- **TraceId**: `179a0f177f3fc40351723994dcc9f6b9`
- **TenantId**: `acme`
- **CorrelationId**: `0HNLU0RL9OVJD`
- **Evidence**: 
  `[10:20:05 INF] [D1 FORENSICS] [API] Request received. TenantId: acme, CorrelationId: 0HNLU0RL9OVJD, TraceId: 179a0f177f3fc40351723994dcc9f6b9`

### Hop 2: Domain Service (`EmployeeService`)
- **Status**: ✅ SUCCESS
- **TenantId**: `acme`
- **Evidence**:
  `[10:20:05 INF] [D1 FORENSICS] [SERVICE] OnboardEmployeeAsync starting. TenantId: acme, ContextTenantId: acme`
  `[10:20:05 INF] [D1 FORENSICS] [SERVICE] About to Publish EmployeeOnboardedIntegrationEvent. TenantId: acme`

### Hop 3: MassTransit Publish Pipeline (`TenantPublishFilter`)
- **Status**: ❌ FAILURE (Skipped)
- **Evidence**: No `[D1 FORENSICS] [PUBLISH FILTER]` log entries were found during the execution.
- **Observation**: The filter is registered at the transport level (e.g., `UsingRabbitMq`), which is bypassed during EF Outbox capture.

### Hop 4: EF Outbox Capture
- **Status**: ❌ FAILURE (Data Lost)
- **MessageId**: `a2a90000-09fc-0200-e782-08debe06eaea`
- **Headers**: `null`
- **Evidence**:
  `[10:20:05 INF] [D1 FORENSICS] [TEST] OutboxMessage count: 1`
  `[10:20:05 INF] [D1 FORENSICS] [TEST] OutboxMessage Id: a2a90000-09fc-0200-e782-08debe06eaea, Headers: null`

## Root Cause Analysis
The execution context is lost at the **EF Outbox Capture Boundary**. 

1. **Transport-Level Filtering**: `TenantPublishFilter` is registered inside the transport configuration block (`UsingRabbitMq` / `UsingInMemory`).
2. **Outbox Interception**: When `AddEntityFrameworkOutbox` is active (even without `UseBusOutbox` in some versions, or when explicitly enabled), MassTransit provides a `ScopedPublishEndpoint` that captures messages *before* they reach the transport pipeline.
3. **Filter Bypass**: Since the transport pipeline is only used during *delivery* (from Outbox to Broker), the filters never execute during *capture* (from Service to Outbox).
4. **Header Exclusion**: The outbox captures only the headers present at the point of the `Publish()` call. Since the filters haven't run yet, and domain code is forbidden from setting headers manually, the outbox row is created with empty/null headers.

## Conclusion
Remediation must move metadata injection **BEFORE** outbox capture. 

**Spike Result (Phase 6)**:
- **Global Scoped Filter**: ✅ SUCCESS (Proven)
- **Finding**: MassTransit 8.3.0 requires filters to be **generic** (`IFilter<PublishContext<T>>`) to be correctly applied to the `ScopedPublishEndpoint` used by the Entity Framework Outbox.
- **Evidence**: 
  `[11:04:58 INF] [D1 FORENSICS] [TEST] OutboxMessage Id: ..., Headers: { "MT-Tenant-Id": "acme", ..., "MT-Diagnostic-Spike": "GenericFilterRan" }`
- **Action**: Refactor all publish/send filters to be generic and register them globally in the transport configuration. This guarantees that execution context metadata is captured in the Outbox row and survives transport to the consumer.

