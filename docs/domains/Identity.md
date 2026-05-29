# Identity Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages registration, login, refresh, logout, JWT creation, refresh tokens, revoked tokens, signing keys, and security audit entries. Evidence: `src/Backend/Karamchari.Identity/IdentityEndpoints.cs:20`, `src/Backend/Karamchari.Identity.Infrastructure/Persistence/IdentityDbContext.cs:21`. |
| Business Objectives | UNKNOWN beyond authentication and token lifecycle. |
| Core Concepts | Register request, login request/response, refresh token, revoked token, signing key metadata, security audit entry. |
| Aggregates / Entities | `RefreshToken`, `RevokedToken`, `SecurityAuditEntry`, `SigningKeyMetadata`. Evidence: `src/Backend/Karamchari.Identity.Infrastructure/Persistence/IdentityDbContext.cs:21`. |
| Value Objects | Identity DTOs. Evidence: `src/Backend/Karamchari.Identity.Contracts/IdentityDtos.cs`. |
| State Machines | Token revocation/expiration states are represented by entities; explicit state machine UNKNOWN. |
| Events | UNKNOWN; no identity integration events found in this pass. |
| Commands | Register, login, refresh, logout. Evidence: `src/Backend/Karamchari.Identity/IdentityEndpoints.cs:22`. |
| Queries | UNKNOWN public query surface beyond auth responses. |
| Business Rules / Invariants / Validation | JWT options, signing key resolver, refresh token service, blacklist service, cleanup workers exist. Exact security policy catalog UNKNOWN. Evidence: `src/Backend/Karamchari.Identity.Infrastructure/Configuration/JwtOptions.cs`, `src/Backend/Karamchari.Identity.Infrastructure/Security/*.cs`, `src/Backend/Karamchari.Identity.Infrastructure/BackgroundServices/*.cs`. |
| Calculation Rules | Token expiry is configurable; exact values are environment-dependent. |
| Ownership Rules | TenantId is present in register request. Evidence: `src/Backend/Karamchari.Identity.Contracts/IdentityDtos.cs:4`. Role/permission authority is in Core; business owner UNKNOWN. |
| Dependencies | ASP.NET Identity, JWT bearer auth, database signing keys, Core tenant/security middleware. |
| External Integrations | UNKNOWN beyond database and JWT libraries. |
| Examples | `POST /api/identity/register`, `POST /api/identity/login`, `POST /api/identity/refresh`, `POST /api/identity/logout`. |
| Failure Scenarios | Revoked-token and refresh-token cleanup workers exist. Identity integration tests exist. Evidence: `tests/Backend/Karamchari.Identity.IntegrationTests/IdentityTests.cs`. |
| Known Limitations | JWT rotation runbook and tested recovery evidence are missing. |
