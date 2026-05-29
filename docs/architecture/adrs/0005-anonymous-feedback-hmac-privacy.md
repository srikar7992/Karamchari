# ADR 0005 — Anonymous Feedback HMAC Privacy Model

- **Status:** Accepted
- **Date:** 2026-05-08
- **Deciders:** Solo founder

## Context

Peer feedback and 360-degree reviews require genuine anonymity to produce honest signal.
Naive approaches (e.g., storing provider_id as nullable and leaving it null for anonymous
submissions) are vulnerable to timing attacks, record count correlation, and accidental
disclosure via admin tooling.

Enterprise HR requirements also mandate:
- HR can decrypt identity in a formal investigation (not routine curiosity).
- Audit trail of all identity-reveal actions.
- No API path that allows any actor to list "who submitted anonymous feedback for X."

## Decision

Anonymous feedback uses **HMAC-SHA256 tokenization** with a tenant-scoped secret.

1. When a `FeedbackSubmission` is created anonymously, the system computes:
   `AnonymityToken = HMAC-SHA256(key: tenant_secret, data: provider_employee_id)`
2. Only `AnonymityToken` is stored on the `FeedbackSubmission` — never `ProviderId`.
3. To reveal identity, HR presents the `AnonymityToken` and a justification;
   the system computes HMAC for every employee and checks equality. This operation
   is logged in the audit trail (who revealed, when, justification text).
4. Threshold visibility: aggregate sentiment is only surfaced once a minimum submission
   count (default: 3) is reached, preventing inference from small groups.
5. No API endpoint exists that returns `AnonymityToken` to any caller other than HR
   with `feedback:reveal` claim. All other endpoints return only the submission content
   and computed scores — never the token.

## Rationale

- HMAC is deterministic (replay-safe), preventing duplicate submissions.
- Tenant-scoped secret means cross-tenant correlation is impossible.
- No storage of provider identity prevents accidental exposure via SELECT *.
- Threshold visibility prevents "only one person reviewed me, score must be theirs."

## Consequences

- Tenant secret must be rotated carefully. After rotation, existing tokens become
  unresolvable until re-HMAC'd with the new key (an offline migration operation).
  Document the rotation procedure before enabling this feature in production.
- HR reveal operations are an audit event; the audit log must be tenant-scoped and
  immutable (append-only, no soft-delete).
- Anonymous feedback cannot be deleted by the submitter after submission. This is
  intentional — once the identity link is severed, there is no safe way to attribute
  a deletion request without re-identifying the submitter.
