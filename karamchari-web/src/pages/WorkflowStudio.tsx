import { useState } from 'react'
import {
  SignalCard,
  RiskCard,
  MapSurface,
  BinduButton,
  LedgerStrip,
} from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'SYS',
  archetype: 'builder',
  signals: 2,
  risks: 1,
  narratives: 0,
  maps: 1,
  bindu: true,
}

interface Node {
  id: string
  kind: 'entry' | 'approval' | 'condition' | 'notify' | 'terminal'
  label: string
  props: { label: string; value: string }[]
}

const NODES: Record<string, Node> = {
  submit: {
    id: 'submit',
    kind: 'entry',
    label: 'Submit via ESS',
    props: [
      { label: 'Trigger', value: 'LeaveRequestCreated' },
      { label: 'Channel', value: 'ESS / Mobile' },
      { label: 'Validation', value: 'Balance + blackout check' },
    ],
  },
  mgr: {
    id: 'mgr',
    kind: 'approval',
    label: 'Manager Approval',
    props: [
      { label: 'Approver', value: 'Direct manager (role)' },
      { label: 'SLA', value: '48 h' },
      { label: 'On Breach', value: 'Escalate: HR partner pool' },
      { label: 'Delegation', value: 'Allowed, recorded' },
    ],
  },
  cond: {
    id: 'cond',
    kind: 'condition',
    label: 'Duration > 3 days?',
    props: [
      { label: 'Expression', value: 'request.days > 3' },
      { label: 'True Path', value: 'HR Partner Review' },
      { label: 'False Path', value: 'Notify Requester' },
    ],
  },
  hr: {
    id: 'hr',
    kind: 'approval',
    label: 'HR Partner Review',
    props: [
      { label: 'Approver', value: 'HR partner (zone)' },
      { label: 'SLA', value: '24 h' },
      { label: 'On Breach', value: 'No fallback — WF-RULE-007' },
    ],
  },
  notify: {
    id: 'notify',
    kind: 'notify',
    label: 'Notify Requester',
    props: [
      { label: 'Channels', value: 'In-app + email' },
      { label: 'Template', value: 'NT-LEAVE-DECISION-02' },
    ],
  },
  close: {
    id: 'close',
    kind: 'terminal',
    label: 'Close',
    props: [
      { label: 'Effects', value: 'Balance debit · calendar block' },
      { label: 'Ledger', value: 'Sutra entry, immutable' },
    ],
  },
}

const PALETTE = ['Approval Step', 'Condition', 'Notification', 'SLA Timer', 'Escalation']

const KIND_STYLE: Record<Node['kind'], string> = {
  entry: 'border-outline-variant',
  approval: 'border-l-2 border-l-yantra-indigo border-outline-variant',
  condition: 'border-dashed border-outline-variant',
  notify: 'border-outline-variant',
  terminal: 'border-l-2 border-l-sthira-forest border-outline-variant',
}

function CanvasNode({
  node,
  selected,
  onSelect,
}: {
  node: Node
  selected: boolean
  onSelect: (id: string) => void
}) {
  return (
    <button
      onClick={() => onSelect(node.id)}
      className={`w-full text-left border bg-surface px-4 py-3 transition-colors ${KIND_STYLE[node.kind]} ${
        selected ? 'bg-surface-container-low outline outline-1 outline-tamra-copper' : 'hover:bg-surface-container-low'
      }`}
    >
      <span className="font-mono text-[9px] uppercase tracking-widest text-on-surface-variant block mb-1">
        {node.kind}
      </span>
      <span className="font-tabular-data text-tabular-data text-primary">{node.label}</span>
    </button>
  )
}

const Arrow = ({ label }: { label?: string }) => (
  <div className="flex flex-col items-center py-1">
    {label && <span className="font-mono text-[9px] text-on-surface-variant uppercase mb-0.5">{label}</span>}
    <span className="text-on-surface-variant leading-none">↓</span>
  </div>
)

export function WorkflowStudio() {
  const [selectedId, setSelectedId] = useState('mgr')
  const selected = NODES[selectedId]

  return (
    <>
      {/* Artifact header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Workflow Definition · <span className="font-mono">LEAVE-STD-01</span>
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Workflow Studio
          </h2>
        </div>
        <span className="px-2 py-1 bg-surface-container-high border border-outline-variant font-mono text-[10px] tracking-wider uppercase self-start md:self-auto">
          Draft v4 · supersedes v3
        </span>
      </div>

      {/* Validation signals */}
      <section className="grid grid-cols-1 sm:grid-cols-2 gap-grid-12 mb-8">
        <SignalCard label="Definition Shape" value="6" unit="steps · 2 branches" note="All paths terminate at Close" bindu="good" />
        <SignalCard label="In-Flight Instances" value="214" note="Complete under v3 on publish (grandfather rule GF-02)" bindu="neutral" />
      </section>

      <div className="grid grid-cols-1 md:grid-cols-12 gap-grid-12 mb-8">
        {/* Palette rail */}
        <aside className="md:col-span-2">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            Palette
          </h3>
          <div className="space-y-2">
            {PALETTE.map((p) => (
              <div
                key={p}
                className="hairline-all px-3 py-2 font-tabular-data text-[12px] text-on-surface-variant cursor-grab hover:bg-surface-container-low hover:text-primary transition-colors"
              >
                {p}
              </div>
            ))}
          </div>
        </aside>

        {/* Canvas */}
        <div className="md:col-span-7">
          <MapSurface kicker="Definition Canvas · LEAVE-STD-01 v4" axes={{ x: 'Entry', y: 'Terminal' }}>
            <div className="max-w-md mx-auto">
              <CanvasNode node={NODES.submit} selected={selectedId === 'submit'} onSelect={setSelectedId} />
              <Arrow />
              <CanvasNode node={NODES.mgr} selected={selectedId === 'mgr'} onSelect={setSelectedId} />
              <Arrow label="approved" />
              <CanvasNode node={NODES.cond} selected={selectedId === 'cond'} onSelect={setSelectedId} />
              <div className="grid grid-cols-2 gap-4 mt-1">
                <div>
                  <Arrow label="yes" />
                  <CanvasNode node={NODES.hr} selected={selectedId === 'hr'} onSelect={setSelectedId} />
                  <Arrow />
                </div>
                <div className="flex flex-col justify-end">
                  <Arrow label="no" />
                </div>
              </div>
              <CanvasNode node={NODES.notify} selected={selectedId === 'notify'} onSelect={setSelectedId} />
              <Arrow />
              <CanvasNode node={NODES.close} selected={selectedId === 'close'} onSelect={setSelectedId} />
            </div>
          </MapSurface>
        </div>

        {/* Inspector */}
        <aside className="md:col-span-3">
          <h3 className="font-mono-label text-mono-label text-on-surface-variant mb-4 uppercase tracking-widest">
            Inspector
          </h3>
          <div className="bg-surface-container-lowest hairline-all p-5">
            <div className="font-mono text-[9px] uppercase tracking-widest text-on-surface-variant mb-1">
              {selected.kind}
            </div>
            <div className="font-tabular-data text-tabular-data text-primary mb-5">{selected.label}</div>
            <div className="space-y-3">
              {selected.props.map((p) => (
                <div key={p.label} className="flex justify-between gap-3 pb-2 hairline-b">
                  <span className="font-tabular-data text-[12px] text-on-surface-variant">{p.label}</span>
                  <span className="font-mono text-[11px] text-primary text-right">{p.value}</span>
                </div>
              ))}
            </div>
          </div>
        </aside>
      </div>

      {/* Validation strip + publish */}
      <section className="bg-surface-container-lowest hairline-all p-6 mb-8">
        <div className="flex flex-col lg:flex-row justify-between lg:items-start gap-8">
          <RiskCard
            className="flex-1"
            severity="elevated"
            title="WF-RULE-007 — Fallback Approver"
            body="Branch 'duration > 3 days' defines no fallback approver for HR Partner Review. On SLA breach the instance escalates to the HR partner pool by engine default; the definition should state this explicitly."
          />
          <div className="lg:w-64 shrink-0 flex flex-col gap-3">
            <BinduButton className="py-3">Publish Workflow</BinduButton>
            <button className="py-3 bg-transparent border border-outline-variant text-on-surface-variant font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-low hover:text-primary transition-colors">
              Save Draft
            </button>
            <p className="font-tabular-data text-[12px] text-on-surface-variant">
              Publishing v4 makes it law for new leave requests. The 214 in-flight instances
              complete under v3; no instance changes rules mid-journey.
            </p>
          </div>
        </div>
      </section>

      <LedgerStrip
        entries={[
          { label: 'Artifact', value: 'LEAVE-STD-01 · draft v4' },
          { label: 'Engine', value: 'PRAVAHA v3' },
          { label: 'Active Version', value: 'v3 since 2025-11-02 · published by S. Menon' },
        ]}
      />
    </>
  )
}
