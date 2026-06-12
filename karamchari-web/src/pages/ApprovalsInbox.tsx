import { useState } from 'react'
import { Icon } from '@/components/shell/Icon'
import { RiskCard, CaseTimeline, LedgerStrip, type TimelineEntry } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'approval',
  signals: 2,
  risks: 1,
  narratives: 0,
  maps: 0,
  bindu: false,
}

interface QueueItem {
  id: string
  kind: string
  requester: string
  summary: string
  age: string
  sla: 'within' | 'near' | 'breached'
  facts: { label: string; value: string }[]
  policy: string
  conflict?: { severity: 'critical' | 'elevated' | 'monitored'; title: string; body: string }
  timeline: TimelineEntry[]
}

const queue: QueueItem[] = [
  {
    id: 'APR-2026-1182',
    kind: 'LEAVE REQUEST',
    requester: 'Ramesh Devarakonda',
    summary: '2 days sick leave · Oct 12–13',
    age: '2h',
    sla: 'within',
    facts: [
      { label: 'Leave Type', value: 'Sick (SL)' },
      { label: 'Duration', value: '2.0 days · Oct 12 – Oct 13' },
      { label: 'Balance After', value: '6.5 days SL remaining' },
      { label: 'Prior Requests (90d)', value: '1 approved, 0 rejected' },
    ],
    policy: 'SL policy v4: no certificate required under 3 days. No blackout period overlaps.',
    conflict: {
      severity: 'monitored',
      title: 'Coverage — Monitored',
      body: 'Zone A coverage remains above minimum (4/3) on both days. No roster conflict.',
    },
    timeline: [
      { ts: '2026-06-12T06:11:02Z', tag: 'SUBMITTED', tone: 'neutral', body: 'Request submitted via ESS. Routed by workflow LEAVE-STD-01 to direct manager.' },
      { ts: '2026-06-12T06:11:03Z', tag: 'ROUTED', tone: 'good', body: 'Single-step approval. SLA: 48h, escalates to HR partner on breach.' },
    ],
  },
  {
    id: 'APR-2026-1179',
    kind: 'EXPENSE CLAIM',
    requester: 'Meera Krishnan',
    summary: '₹4,500 · Client dinner, Q3 offsite',
    age: '5h',
    sla: 'within',
    facts: [
      { label: 'Category', value: 'Client Entertainment' },
      { label: 'Amount', value: '₹4,500.00' },
      { label: 'Receipts', value: '1 attached, OCR verified' },
      { label: 'Category Budget (Q3)', value: '₹38,200 of ₹60,000 used' },
    ],
    policy: 'Expense policy v7: client entertainment ≤ ₹5,000 single-approver. Receipt mandatory — present.',
    timeline: [
      { ts: '2026-06-12T03:24:40Z', tag: 'SUBMITTED', tone: 'neutral', body: 'Claim submitted with receipt. Routed by workflow EXP-STD-02.' },
    ],
  },
  {
    id: 'APR-2026-1164',
    kind: 'SHIFT SWAP',
    requester: 'Vikram Singh ↔ Priya Sharma',
    summary: 'Oct 15, evening shift exchange',
    age: '1d',
    sla: 'near',
    facts: [
      { label: 'Shift', value: 'Oct 15 · 14:00–22:00 · Zone B' },
      { label: 'Skill Parity', value: 'Both QA-certified for floor duty' },
      { label: 'Overtime Impact', value: 'None — both within weekly hours' },
      { label: 'Acceptance', value: 'Counterparty accepted 2026-06-11' },
    ],
    policy: 'Swap policy v2: same-skill swaps auto-validated; supervisor approval confirms coverage.',
    conflict: {
      severity: 'elevated',
      title: 'SLA — Approaching',
      body: 'Decision due within 7h. On breach, swap escalates to shift coordinator and requester is notified.',
    },
    timeline: [
      { ts: '2026-06-11T09:02:11Z', tag: 'REQUESTED', tone: 'neutral', body: 'Swap requested by Vikram S. via Swap Center.' },
      { ts: '2026-06-11T16:40:09Z', tag: 'ACCEPTED', tone: 'good', body: 'Priya S. accepted the exchange. Pending supervisor confirmation.' },
    ],
  },
]

const SLA_TONE = { within: 'good', near: 'caution', breached: 'rakta-critical' } as const

export function ApprovalsInbox() {
  const [selectedId, setSelectedId] = useState(queue[0].id)
  const selected = queue.find((q) => q.id === selectedId) ?? queue[0]

  return (
    <>
      {/* Header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Unified Decision Queue · Flows &amp; Policies
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Approvals Inbox
          </h2>
        </div>
        <div className="flex gap-8 font-mono text-[12px] text-on-surface-variant">
          <span>
            PENDING <span className="text-primary text-[16px] ml-1">{queue.length}</span>
          </span>
          <span>
            NEAR SLA <span className="text-pita-caution text-[16px] ml-1">1</span>
          </span>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12">
        {/* Queue rail */}
        <div className="md:col-span-4 space-y-3">
          {queue.map((item) => (
            <button
              key={item.id}
              onClick={() => setSelectedId(item.id)}
              className={`w-full text-left p-4 transition-colors ${
                item.id === selectedId
                  ? 'bg-surface-container-low hairline-all border-l-2 border-l-primary'
                  : 'hairline-all bg-surface-container-lowest hover:bg-surface-container-low'
              }`}
            >
              <div className="flex justify-between items-start mb-2">
                <span className="font-mono-label text-mono-label text-on-surface-variant">{item.kind}</span>
                <span className="flex items-center gap-2 font-mono text-[10px] text-on-surface-variant">
                  <span className={`bindu ${SLA_TONE[item.sla]}`} />
                  {item.age}
                </span>
              </div>
              <p className="font-medium text-primary">{item.requester}</p>
              <p className="text-sm text-on-surface-variant mt-1">{item.summary}</p>
            </button>
          ))}
        </div>

        {/* Evidence panel */}
        <div className="md:col-span-8 bg-surface-container-lowest hairline-all p-6 flex flex-col">
          <div className="flex justify-between items-start hairline-b pb-4 mb-6">
            <div>
              <div className="font-mono text-[11px] text-on-surface-variant mb-1">{selected.id}</div>
              <h3 className="font-pull-quote text-pull-quote text-primary !text-2xl">{selected.kind}</h3>
            </div>
            <span className="px-2 py-1 bg-surface-container-high border border-outline-variant font-mono text-[10px] tracking-wider uppercase">
              Awaiting Decision
            </span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-4 font-tabular-data text-tabular-data mb-6">
            {selected.facts.map((f) => (
              <div key={f.label} className="pb-2 border-b border-surface-variant border-dashed">
                <div className="text-[12px] text-on-surface-variant mb-1">{f.label}</div>
                <div className="text-primary">{f.value}</div>
              </div>
            ))}
          </div>

          <div className="bg-surface p-4 hairline-all mb-6">
            <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-2 flex items-center gap-2">
              <Icon name="policy" className="!text-[14px]" /> Policy Context
            </div>
            <p className="font-tabular-data text-tabular-data text-primary">{selected.policy}</p>
          </div>

          {selected.conflict && (
            <RiskCard
              className="mb-6"
              severity={selected.conflict.severity}
              title={selected.conflict.title}
              body={selected.conflict.body}
            />
          )}

          {/* Decision strip — repeated acts of the role stay ink (archetype: approval) */}
          <div className="flex gap-3 mb-8">
            <button className="flex-1 bg-primary text-on-primary font-mono-label text-mono-label py-3 uppercase tracking-widest hover:opacity-90 transition-opacity">
              Approve
            </button>
            <button className="flex-1 bg-transparent border border-outline-variant text-primary font-mono-label text-mono-label py-3 uppercase tracking-widest hover:bg-surface-container-low transition-colors">
              Reject
            </button>
            <button className="px-6 bg-transparent border border-outline-variant text-on-surface-variant font-mono-label text-mono-label py-3 uppercase tracking-widest hover:bg-surface-container-low transition-colors">
              Delegate
            </button>
          </div>

          <div className="hairline-t pt-6">
            <div className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-4">
              Decision Timeline
            </div>
            <CaseTimeline entries={selected.timeline} />
          </div>
        </div>
      </div>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Queue', value: 'Approvals.My' },
          { label: 'Workflow Engine', value: 'PRAVAHA v3' },
          { label: 'Synced', value: '2026-06-12T08:04:10Z' },
        ]}
      />
    </>
  )
}
