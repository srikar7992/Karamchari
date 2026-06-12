# Doctrine Exceptions Register

Every doctrine violation that ships must be recorded here first. An exception without
all six fields is invalid. An expired exception is a build failure, not a grace period.
Exceptions are re-approved or removed at expiry — never silently renewed.

Rules:
1. One exception per rule per surface. No blanket exemptions ("all executive screens").
2. Owner is a person or council, never a team alias.
3. Expiry maximum 12 months from grant.
4. Replacement plan is mandatory — an exception is a debt, not a right.
5. `check-doctrine.mjs` honors active exceptions and fails on expired ones.

## Format

```text
Exception ID: EX-NNN
Rule:        <doctrine section violated>
Surface:     <page file>
Reason:      <why violation is necessary>
Owner:       <person/council>
Granted:     YYYY-MM-DD
Expires:     YYYY-MM-DD
Replacement: <plan to remove the exception>
```

## Active Exceptions

| ID | Rule | Surface | Owner | Granted | Expires |
|---|---|---|---|---|---|
| — | — | — | — | — | — |

None. The register being empty is the healthy state.

## Expired / Retired

| ID | Rule | Surface | Outcome |
|---|---|---|---|
| — | — | — | — |
