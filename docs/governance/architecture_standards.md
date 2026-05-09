# Architecture Standards & Platform Guardrails

## 1. Dependency Rules
- **Domain Logic:** Zero infrastructure dependencies. No EF Core, no HTTP Clients.
- **BFF/Controllers:** No business rules. Controllers only map HTTP to Commands/Queries.
- **Cross-Module:** Modules must communicate via Integration Events or explicit Contracts. No shared database joins across bounded contexts.

## 2. Multi-Tenant Rules
- **No Bypass:** Tenant isolation is absolute. `ITenantProvider` must resolve from JWT.
- **No Fallbacks:** Never use `"default"` or `"system"` or `"00000...0"` as a tenant ID. If tenant is missing, return `401 Unauthorized`.

## 3. Transactional Integrity
- Domain Events must be saved in the **same transaction** as the aggregate using the Outbox pattern.
- State-changing operations (POST/PUT/DELETE) must be guarded by `IdempotencyFilter`.

## 4. Enterprise Audit Governance
- All state changes must be intercepted by `AuditInterceptor`.
- Audit logs are immutable and append-only.
