# Archetype: Command Center

ID: `command-center`
Question: *Is the operation healthy, and what do I run next?*

Operational control of a live process: a payroll cycle, the attendance day, platform
health. The ISRO mission-control register applies most literally here — current
state, anomaly queue, and controlled actions with irreversible steps clearly gated.

## Zones

1. **State header** — the process and its phase ("PAY PERIOD 2026-06 · CALCULATION
   LOCKED") in mono, with a status bindu. The phase is always knowable at a glance.
2. **Health row** — SignalCards: throughput, exceptions, lag, coverage. The one
   permitted `animate-pulse` lives here when a run is in flight.
3. **Anomaly queue** — InstitutionalTable of exceptions with resolve/dismiss ink
   actions per row.
4. **Run controls** — the process's verbs in phase order (Start → Lock → Approve →
   Publish). Completed phases ink-disabled; future phases hairline; the current
   decisive phase is the Bindu.
5. **Run ledger** — CaseTimeline of this cycle's acts: who locked, who approved, when.

## Classification profile

Signals up to persona budget (SYS: 4), Risks 1–3 (the anomaly classes), Narratives
0–1, Maps 0–1.

## Bindu

Exactly one: the current phase's decisive act ("Lock Run", "Initiate Disbursement",
"Finalize Period"). The Bindu moves through the phase sequence as the cycle advances —
one copper action at any moment.

## Component manifest

`SignalCard`, `InstitutionalTable`, `CaseTimeline`, `RiskCard`, `BinduButton`,
`LedgerStrip`.

## Used by

Payroll Cockpit, Payroll Run Console, Disbursement Center, Period Finalization,
Ops Health & Metrics, System Pulse, Team Attendance Board, Attendance Ops (frontline),
Data Migration Console, Accrual & Carry-Forward Console.

## Anti-patterns

- Alarm theater on anomalies (DOCTRINE §10). Exceptions queue calmly in rakta hairlines.
- Ambiguous phase. If an operator must ask "did it lock?", the screen failed.
- Celebration on publish. Authority transfers silently; the ledger records it.
