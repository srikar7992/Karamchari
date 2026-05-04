# Karamchari Portal

Next.js 15 App Router operator portal for Karamchari. Pulls live data from the
.NET BFF (`Karamchari.Api`) at `http://localhost:5000` in dev.

## Quick start

```bash
cd src/Frontend/portal
npm install
npm run dev
```

Then open <http://localhost:3000>. You'll be redirected to `/dashboard`.
The `/directory` page hits the BFF and renders departments live.

The BFF must be running locally too (`dotnet run --project src/Backend/Karamchari.Api`),
with `Tenancy:TrustedGatewayFingerprint` set to whatever `NEXT_PUBLIC_GATEWAY_FINGERPRINT`
is configured to (default `local-dev-gateway` matches the value in
`appsettings.Development.json`).

## How tenant resolution works in dev

Every API call sent by `src/lib/api/client.ts` carries two headers:

| Header                 | Value source                                | Purpose                                                                 |
| ---------------------- | ------------------------------------------- | ----------------------------------------------------------------------- |
| `X-Tenant-Id`          | `NEXT_PUBLIC_DEFAULT_TENANT_ID`             | Tells the BFF which tenant context to use.                              |
| `X-Karamchari-Gateway` | `NEXT_PUBLIC_GATEWAY_FINGERPRINT`           | Proves the request came through a trusted edge (impersonates APIM).      |

The .NET `HttpTenantProvider` only honours `X-Tenant-Id` when the gateway proof
matches the configured fingerprint. Without it, every request 401s. **In
production, neither header originates in the SPA** — the tenant comes from the
user's signed JWT and the gateway proof is injected by APIM's policy.

## Stack

- **Next.js 15** (App Router, Server Components by default)
- **React 19**
- **TypeScript 5.7**
- **TanStack Query v5** for data fetching, caching, devtools
- **Tailwind CSS 3.4** with the Monolith Precision token set (see `docs/design/stitch/monolith_precision/DESIGN.md`)
- Hand-rolled shadcn-style primitives (Table, Card, Button, StatusDot)

## Layout

```
portal/
├── src/
│   ├── app/
│   │   ├── layout.tsx           # Root layout: Inter font, QueryClientProvider, dark class
│   │   ├── page.tsx             # / -> /dashboard
│   │   ├── providers.tsx        # 'use client' QueryClient setup
│   │   ├── globals.css          # Tailwind base + Monolith Precision tokens
│   │   ├── dashboard/page.tsx   # Static port of the Stitch dashboard
│   │   └── directory/page.tsx   # Live: useDepartments + shadcn Table
│   ├── components/
│   │   ├── shell/               # AppShell, Sidebar, Topbar
│   │   └── ui/                  # Table, Card, Button, StatusDot
│   └── lib/
│       ├── api/                 # client (fetch wrapper), config, errors
│       ├── hooks/               # useDepartments, useCreateDepartment
│       ├── types/               # Department DTO mirrors of BFF projections
│       └── utils/               # cn() classname helper
├── tailwind.config.ts
├── tsconfig.json
└── next.config.ts
```

## Conventions

- **Always use the central `api` client** from `@/lib/api/client`. Never call `fetch` directly from a hook or component — bypassing it loses the tenant headers and the typed error mapping.
- **Mutations invalidate**, not refetch. See `useCreateDepartment` for the pattern.
- **Server components are the default**; only use `"use client"` where state, effects, or browser APIs are required.
- **DTOs live in `src/lib/types/`** and are hand-mirrored from the BFF projections under `Karamchari.Api/Models/`. There is no codegen yet — keep them in sync manually until we add OpenAPI generation.
