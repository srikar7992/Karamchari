# Tenant Execution Flow

```mermaid
sequenceDiagram
    participant User as External Request
    participant GW as API Gateway / BFF
    participant MW as Tenant Middleware / Filter
    participant CTX as TenantExecutionContext
    participant BL as Business Logic
    participant DB as SQL Server (RLS)

    User->>GW: HTTP Request (JWT / Header)
    GW->>MW: Invoke Pipeline
    MW->>MW: Extract TenantExecutionEnvelope
    MW->>CTX: Establish(envelope)
    CTX-->>MW: IDisposable Scope
    MW->>BL: Execute Operation
    activate BL
    BL->>DB: EF Core Query
    activate DB
    DB->>DB: EXEC sp_set_session_context @TenantId
    DB-->>BL: Filtered Results (Schema Isolated)
    deactivate DB
    deactivate BL
    MW->>CTX: Dispose Scope
    MW-->>User: HTTP Response
```

## How to Debug
1.  **Missing Tenant**: Exception `TenantResolutionException` at **Middleware** level. Check JWT claims or `X-Tenant-Id` header.
2.  **Cross-Tenant Leak**: Exception `PooledConnectionContaminationException` at **DB** level. Check for unawaited Tasks or background thread escape.
3.  **Traceability**: Search Seq for `karamchari.tenant.id`. Every step above is automatically tagged.
