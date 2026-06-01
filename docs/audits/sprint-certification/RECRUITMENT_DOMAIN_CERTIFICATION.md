# Recruitment Domain Certification
**Date:** 2026-06-01  
**Status:** CERTIFIED

---

## Aggregate Coverage

| Aggregate | Tests | Invariants Verified | Domain Events | Status |
|---|---|---|---|---|
| JobRequisition | 21 | Draft→Published state machine, approve/reject rules | RequisitionPublished | CERTIFIED |
| Candidate | 88 | Create, snapshot, versioning, withdraw | CandidateWithdrawn | CERTIFIED |
| Application | 131 | Apply, advance, withdraw, duplicate guard | ApplicationAdvanced | CERTIFIED |
| Interview | 95 | Schedule, reschedule, cancel, feedback | FeedbackSubmitted | CERTIFIED |
| Offer | 94 | Draft→Approved→Issued→Accepted, expiry | OfferAccepted | CERTIFIED |

## State Machine Verification

### JobRequisition
- Draft → Approved (via Approve())
- Draft → Rejected (via Reject())
- Approved → Published (via Publish())
- Published → Closed (via Close())
- Any state → Cancelled

Tested: approve-twice guard, publish-twice guard, invalid transitions.

### Application
- Applied → Screening → Interviewing → Offered
- Any state → Withdrawn

Tested: duplicate apply guard (same candidate, same requisition).

### Offer
- Draft → Approved → Issued → Accepted/Rejected
- Issued → Expired (time-based)

Tested: accept-twice guard, expired offer rejection, accepted offer immutability.

## Idempotency

- `IdempotencyTests.OfferAcceptShouldBeIdempotent` — domain rule rejects second Accept
- `IdempotencyTests.HireCandidateShouldBeIdempotent` — domain rule rejects second Hire

## Integration Evidence

- Full vertical slice E2E: `VerticalSlice_EndToEnd_ShouldSucceed`
- Multi-tenant isolation: `MultiTenant_TenantA_ShouldNotSeeTenantBData`
- Authorization enforcement: `Authorization_Manager_ShouldBeDenied_Hiring`

## Certification Decision

**CERTIFIED**
