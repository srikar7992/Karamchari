# Institutional Lawbook

Every rule that affects execution gets an identifier here. Surfaces, workflow
nodes, and lifecycle transitions cite law IDs, never restate rules in prose
alone. A law is institutional ("certificate required from the third day"), not
implementation ("the modal validates input").

Governance: `check-doctrine.mjs` verifies every law whose Enforcement column
includes `web` is cited somewhere in `karamchari-web/src/`. A registered law
nobody cites is dead text and fails the build — register laws when a surface
needs them, not speculatively. Amending a law is a new version of the law, not
an edit; in-flight processes complete under the version they started with
(that principle is itself GF-02).

## Register

| ID | Statement | Domain | Enforcement |
|---|---|---|---|
| GF-02 | A published workflow version never alters in-flight instances; instances complete under the version they started on. | Workflow | web + engine |
| WF-RULE-007 | Every conditional approval branch states its fallback approver explicitly; engine-default escalation must be declared, not implied. | Workflow | web + engine |
| PAY-AUTH-04 | Approving a payroll run requires payroll approval authority; the approval act records actor and authority in the run ledger. | Payroll | web + engine |
| REST-11H | Eleven hours minimum rest between consecutive shifts; rosters violating it cannot publish without a registered exception. | Scheduling | web + engine |
| LV-SLA-02 | Leave approval decisions are due within 48 hours; on breach the request escalates to the HR partner pool and the requester is notified. | Leave | web + engine |
| LV-CERT-03 | Sick leave of three or more consecutive days requires a medical certificate before approval. | Leave | web + engine |
| LV-CF-01 | Earned leave carries forward up to 30 days at period close; the excess lapses and the lapse is ledgered. | Leave | web + engine |
| EXP-AUTH-01 | Expense claims at or below ₹5,000 require a single approver; above that, two-step approval with finance review. | Financial Ops | web + engine |
| SWP-SLA-01 | Shift swap confirmations are due within 48 hours of counterparty acceptance; on breach the swap escalates to the shift coordinator. | Scheduling | web + engine |
| OFF-AUTH-01 | Approving an offer requires hiring authority for the requisition; compensation above band P75 additionally requires CHRO endorsement, recorded in the offer ledger. | Recruitment | web + engine |
| HIRE-ID-01 | An employee identity is created only from an accepted offer; the hire act records candidate, offer version, actor, and authority. | Recruitment | web + engine |

## Amendment log

| Date | Change |
|---|---|
| 2026-06-12 | Lawbook constituted with nine laws already cited or governed on shipped surfaces; web citation check added to doctrine court |
| 2026-06-12 | OFF-AUTH-01 and HIRE-ID-01 registered for Sprint E recruitment surfaces (Candidate Detail, Offer Management) |
