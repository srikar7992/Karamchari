import { useMemo, useState } from 'react'
import { CaseTimeline, LedgerStrip, type TimelineEntry } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'timeline',
  signals: 1,
  risks: 0,
  narratives: 0,
  maps: 0,
  bindu: false,
}

type Channel = 'All' | 'Approvals' | 'Payroll' | 'Attendance' | 'Recognition'

const entries: (TimelineEntry & { channel: Channel; unread?: boolean })[] = [
  {
    ts: '2026-06-12T07:55:18Z',
    tag: 'APPROVAL',
    tone: 'good',
    channel: 'Approvals',
    unread: true,
    body: <>Your leave request <span className="font-mono text-[11px]">LR-2026-0871</span> (Jun 19, 1 day) was approved by Ananya R. Balance after: 13 days.</>,
  },
  {
    ts: '2026-06-12T06:30:00Z',
    tag: 'PAYROLL',
    tone: 'neutral',
    channel: 'Payroll',
    unread: true,
    body: <>Payslip for period <span className="font-mono text-[11px]">2026-05</span> is available. Net credited via batch <span className="font-mono text-[11px]">DSB-2026-051</span>.</>,
  },
  {
    ts: '2026-06-11T15:12:44Z',
    tag: 'RECOG',
    tone: 'copper',
    channel: 'Recognition',
    body: <>Precision Badge awarded by Operations Lead: "flawless checkpoint adherence across the full cycle."</>,
  },
  {
    ts: '2026-06-11T09:02:11Z',
    tag: 'ATTEND',
    tone: 'caution',
    channel: 'Attendance',
    body: <>Regularization needed: missing checkout on Jun 10. Submit by Jun 14 or the day records as a half-day absence.</>,
  },
  {
    ts: '2026-06-10T18:45:00Z',
    tag: 'APPROVAL',
    tone: 'neutral',
    channel: 'Approvals',
    body: <>Your shift swap request with Priya S. (Oct 15) is awaiting supervisor confirmation. SLA: 48h.</>,
  },
  {
    ts: '2026-06-09T10:00:00Z',
    tag: 'PAYROLL',
    tone: 'neutral',
    channel: 'Payroll',
    body: <>IT declaration window for FY 2026–27 closes Jun 30. Current declaration status: draft.</>,
  },
]

const channels: Channel[] = ['All', 'Approvals', 'Payroll', 'Attendance', 'Recognition']

export function NotificationCenter() {
  const [channel, setChannel] = useState<Channel>('All')
  const filtered = useMemo(
    () => entries.filter((e) => channel === 'All' || e.channel === channel),
    [channel]
  )
  const unread = entries.filter((e) => e.unread).length

  return (
    <>
      {/* Scope header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Personal Event Trail · Workforce Awareness
          </p>
          <h2 className="font-section-title text-section-title !text-4xl text-primary tracking-tight">
            Notification Center
          </h2>
        </div>
        <div className="flex items-center gap-6">
          <span className="font-mono text-[12px] text-on-surface-variant">
            UNREAD <span className="text-tamra-copper text-[16px] ml-1">{unread}</span>
          </span>
          <button className="px-4 py-2 bg-transparent border border-outline-variant text-primary font-mono-label text-mono-label uppercase tracking-wider hover:bg-surface-container-low transition-colors">
            Mark All Read
          </button>
        </div>
      </div>

      {/* Query strip */}
      <div className="flex flex-wrap gap-2 mb-8">
        {channels.map((c) => (
          <button
            key={c}
            onClick={() => setChannel(c)}
            className={`px-3 py-2 font-mono-label text-mono-label uppercase tracking-wider transition-colors ${
              channel === c
                ? 'bg-primary text-on-primary'
                : 'hairline-all text-on-surface-variant hover:bg-surface-container-low'
            }`}
          >
            {c}
          </button>
        ))}
      </div>

      {/* The trail */}
      <div className="bg-surface-container-lowest hairline-all p-6">
        <CaseTimeline entries={filtered} />
        <button className="w-full mt-8 py-2 border border-outline-variant text-center font-mono text-[10px] uppercase tracking-widest text-on-surface-variant hover:bg-surface-container-low hover:text-primary transition-colors">
          Load Older Entries
        </button>
      </div>

      <LedgerStrip
        className="mt-8"
        entries={[
          { label: 'Inbox', value: 'Notifications.Inbox' },
          { label: 'Hub', value: '/hubs/notifications · connected' },
          { label: 'Synced', value: '2026-06-12T08:05:51Z' },
        ]}
      />
    </>
  )
}
