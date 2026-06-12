# Institutional Lifecycles

The platform's value is measured in journeys, not screens. A lifecycle is an
ordered chain of surfaces an institutional process moves through from creation
to completion. This register lists each lifecycle and its stages as page ids.

Governance: `check-doctrine.mjs` computes lifecycle status from this table —
a stage counts as implemented when its page id exists in the `PageId` union in
`src/lib/nav.ts`. There is no status column to maintain by hand: Complete,
Partial, and Not Started are derived, so this document cannot drift from the
code. Stage ids for unbuilt screens are the ids those screens will take.

## Register

| Lifecycle | Stages (page ids, in order) |
|---|---|
| Hire: Candidate → Employee | candidates, candidate-detail, offer-management, onboarding, employee-360 |
| Leave: Policy → Approval → Audit | leave-policies, approval-rules, approvals, notifications |
| Workflow: Define → Execute → Audit | workflow-studio, approvals, notifications |
| Schedule: Definition → Roster → Coverage | shift-definitions, roster, team-attendance |
| Attendance → Payroll → Payslip | attendance, period-finalization, payroll-run, payslip-browser |
| Asset: Registry → Assignment → Return | asset-registry, asset-detail, asset-assignment |

## Reading the verdict

`Complete` — every stage shipped; the journey can move end to end inside the
system. `Partial` — the chain breaks at an unbuilt stage. `Not Started` — no
stage exists. The next milestone (Sprint F) is the first full pass of the Hire
lifecycle chained into Attendance → Payroll → Payslip: one human moving from
external candidate to paid employee without leaving the system.
