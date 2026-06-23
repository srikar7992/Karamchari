# Frontend Strategy — Phase 1B Declaration

> Effective: 2026-06-23
> Status: **DECISION — binding for Phase 1C onward**

## Decision

Two React applications exist in the repository. Their roles are formalised below.
No new feature work will land in `karamchari-web` after this point.

| Application | Location | Stack | Role | Phase 1 Status |
|---|---|---|---|---|
| **Portal** | `src/Frontend/portal/` | Next.js 15 App Router, React 19, TanStack Query v5, Radix UI, Tailwind | **Production frontend** | Active — all Phase 1C integration lands here |
| **Reference UI** | `karamchari-web/` | Vite + React, hash-based router, `@/lib/offer-lifecycle.ts` state machines | **Design system + UX reference only** | **Frozen** — no new feature work |

## Rationale

- Portal already carries the typed API client, react-query wiring, and the
  hand-written fetch client (`src/lib/api/client.ts`). It is the only tree
  whose source of truth is the live BFF.
- `karamchari-web` was the prototype surface. It contains mock-only Recruitment,
  Payroll, Attendance, and Workflow screens. Continuing to convert its mocks
  in place would re-introduce the very prototype→product ambiguity the audit
  flagged (29 pages / 7 live). Maintaining both trees compounds architectural
  debt every sprint.
- The Recruitment lifecycle law in `karamchari-web/src/lib/offer-lifecycle.ts`
  remains the canonical UX reference; the Next portal must mirror its
  `TRANSITIONS` map and `STATE_LABELS` but source lifecycle state from
  backend projections (`GET /api/v1/recruitment/offers/{id}`,
  `/applications/{id}`, `/candidates/{id}/timeline`) rather than React state.

## Permitted uses of karamchari-web going forward

1. Copy UX patterns and visual language into `src/Frontend/portal/`.
2. Compare against portal behaviour for visual regression reference.
3. Port the `offer-lifecycle.ts` / `payroll-lifecycle.ts` transition maps
   into typed portal modules — verbatim logic, backend-backed state.

## Prohibited in karamchari-web going forward

- New page components.
- New API integrations.
- Bug fixes that do not also apply to portal (cherry-pick rules apply).
- New dependencies.

## Phase 1C integration targets (Portal)

| Screen | Portal route (proposed) | Backend read | Backend writes |
|---|---|---|---|
| Candidate Pipeline | `/recruitment/pipeline` | `GET /api/v1/recruitment/pipeline` | `POST /applications/{id}/advance` |
| Candidate Detail | `/recruitment/candidates/{id}` | `GET /api/v1/recruitment/candidates/{id}` + `/timeline` | `POST /interviews`, `/offers` (creation paths) |
| Offer Management | `/recruitment/offers/{id}` | `GET /api/v1/recruitment/offers/{id}` | `POST /offers/{id}/approve`, `/issue`, `/accept` |

## Enforcement

- Pull requests touching `karamchari-web/**` will be rejected unless they
  fall under the permitted uses above.
- Recruitment Phase 1C work MUST land in `src/Frontend/portal/`.
- The portal must use the typed API client (`src/lib/api/client.ts`) and
  TanStack Query; no inline `fetch` calls inside page components.