# Developer Authentication Guide

**Audience:** any engineer who just cloned Karamchari and wants to call the API. No tribal knowledge required.

## TL;DR — log in and get a token
```bash
curl -s -X POST http://localhost:8080/api/identity/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@dev.local","password":"Dev@Pass123!","tenantId":"dev"}'
# -> { "accessToken": "<JWT>", "refreshToken": "...", "expiresAt": "..." }
```
Then call any endpoint with `Authorization: Bearer <accessToken>`.

## Seeded developer accounts (created by `--provision-dev-tenants`)
Four personas exist in **every** local tenant — `dev`, `acme`, `contoso`, `globex`:

| Email | Role | Password |
|---|---|---|
| `admin@{tenant}.local` | Admin | `Dev@Pass123!` |
| `manager@{tenant}.local` | Manager | `Dev@Pass123!` |
| `employee@{tenant}.local` | Employee | `Dev@Pass123!` |
| `readonly@{tenant}.local` | ReadOnly | `Dev@Pass123!` |

e.g. `admin@dev.local`, `manager@acme.local`, `employee@contoso.local`, `admin@globex.local`.
All 16 accounts were runtime-verified to log in and return a JWT (Module/API/DX certification, 2026-05-30).

> **Local credentials only.** These accounts and `Dev@Pass123!` exist solely for local development
> (`setup-local.sh` / `--provision-dev-tenants`). They are never seeded in non-dev environments.

## How it works
- **Identity model:** ASP.NET Core Identity (`IdentityUser<Guid>`). The user's tenant is stored as a
  `tenant_id` claim; roles are real Identity roles (Admin/Manager/Employee/ReadOnly).
- **Login** (`POST /api/identity/login`): validates the password, reads the `tenant_id` claim, and issues
  a JWT (HS256, `tenant_id` + roles, ~60 min) plus a refresh token.
- **JWT is authoritative for tenancy** — the `tenant_id` claim drives schema selection + RLS. A client
  cannot override it with a header unless a trusted-gateway fingerprint is configured.
- **Refresh:** `POST /api/identity/refresh` with `{ "refreshToken": "..." }` → new access + rotated refresh.
- **Logout:** `POST /api/identity/logout` (authenticated) revokes the refresh token.
- **Register a new user:** `POST /api/identity/register` `{ email, password, tenantId }` (the email must be
  unique; the tenant must be provisioned).

## Seeding / re-seeding
```bash
# one-shot: provision dev/acme/contoso/globex schemas + RLS + the 16 users (idempotent)
docker compose -f infrastructure/local/docker-compose.yml run --rm --no-deps \
  karamchari.api --provision-dev-tenants
```
`setup-local.sh` runs this for you on first setup.

## Known limitation (see ENDPOINT_GAP_ANALYSIS.md)
JWTs are currently issued with an **empty permission set** (roles only). Endpoints gated by a fine-grained
*permission* (e.g. `capability.read`) therefore return **403 even for `admin@…`**. Role→permission mapping
is not yet wired at login. Until it is, use role-gated endpoints; permission-gated ones are not reachable.
