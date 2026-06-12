import { Icon } from '@/components/shell/Icon'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'registry',
  signals: 5,
  risks: 0,
  narratives: 0,
  maps: 1,
  bindu: true,
}

export function CandidatePipeline() {
  return (
    <>
      {/* Requisition header */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 pb-8 border-b border-outline-variant mb-8">
        <div>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-3">
            Req-2026-089A — Active Pipeline
          </p>
          <h2 className="font-pull-quote text-pull-quote text-primary">Senior Systems Architect</h2>
          <div className="flex flex-wrap items-center gap-6 mt-4 text-on-surface-variant font-tabular-data text-tabular-data">
            <div className="flex items-center gap-2">
              <Icon name="location_on" className="!text-[16px]" />
              <span>Bangalore (Hybrid)</span>
            </div>
            <div className="flex items-center gap-2">
              <Icon name="business_center" className="!text-[16px]" />
              <span>Infrastructure Core</span>
            </div>
            <div className="flex items-center gap-2">
              <span className="bindu good" />
              <span>Accepting Candidates</span>
            </div>
          </div>
        </div>
        <div className="flex items-center gap-3">
          <button className="px-5 py-3 border border-outline-variant text-primary font-mono-label text-mono-label uppercase hover:bg-surface-container-high transition-colors">
            Manage Posting
          </button>
          <button className="px-5 py-3 bg-primary text-on-primary font-mono-label text-mono-label uppercase hover:bg-inverse-surface transition-colors flex items-center gap-2">
            <Icon name="add" className="!text-[16px]" />
            Add Candidate
          </button>
        </div>
      </div>

      {/* Kanban */}
      <div className="overflow-x-auto pb-4">
        <div className="flex gap-grid-12 items-stretch min-h-[480px]">
          {/* Applied */}
          <Column title="Applied" count={12}>
            <Card
              name="Arun Sharma"
              match="68%"
              matchTone="bg-pita-caution"
              body="Former Lead at TechCorp. Strong background in legacy system migration, exploring modern microservices architectures."
              footerLeft="Applied 2d ago"
              footerRight={<Icon name="attachment" className="!text-[18px] text-outline" />}
            />
            <Card
              name="Kavya Reddy"
              match="74%"
              matchTone="bg-pita-caution"
              body="Platform engineer with strong observability background. Resume flagged for architecture depth review."
              footerLeft="Applied 4d ago"
              footerRight={<Icon name="attachment" className="!text-[18px] text-outline" />}
            />
          </Column>

          {/* Screened */}
          <Column title="Screened" count={4}>
            <Card
              name="Meera Nair"
              match="92%"
              matchTone="bg-sthira-forest"
              body="Exceptional technical screen. Mastered the distributed systems whiteboard exercise. Clear communicator."
              footerLeft="Passed: Tech L1"
              footerRight={
                <div className="w-6 h-6 rounded-full bg-outline-variant border border-surface flex items-center justify-center text-[10px] font-bold">
                  AK
                </div>
              }
            />
          </Column>

          {/* Interview */}
          <Column title="Interview" count={2}>
            <div className="bg-surface-container-lowest border border-outline-variant p-4 cursor-pointer hover:border-yantra-indigo transition-colors corner-tick relative overflow-hidden">
              <div className="absolute left-0 top-0 bottom-0 w-1 bg-tamra-copper" />
              <div className="flex justify-between items-start mb-4 pl-1">
                <h4 className="font-pull-quote text-pull-quote text-primary !text-[24px] leading-tight">
                  Vikram Desai
                </h4>
                <div className="flex items-center gap-1 bg-surface-container-high px-2 py-1 border border-outline-variant">
                  <span className="w-1.5 h-1.5 rounded-full bg-tamra-copper" />
                  <span className="font-tabular-data text-tabular-data text-[12px]">Match: 88%</span>
                </div>
              </div>
              <div className="bg-surface p-2 mb-4 border border-outline-variant flex items-center gap-2">
                <Icon name="calendar_today" className="!text-[16px] text-tamra-copper" />
                <span className="font-tabular-data text-tabular-data text-[13px] text-primary">
                  Panel: Tomorrow, 2:00 PM
                </span>
              </div>
              <div className="flex justify-between items-center border-t border-outline-variant pt-3">
                <span className="font-mono-label text-mono-label text-on-surface-variant">Round 3 / 4</span>
              </div>
            </div>
          </Column>

          {/* Offer */}
          <Column title="Offer" count={1}>
            <div className="h-full min-h-[200px] flex flex-col items-center justify-center p-6 text-center border border-dashed border-outline-variant opacity-50">
              <Icon name="drafts" className="!text-[32px] text-outline mb-2" />
              <p className="font-mono-label text-mono-label text-on-surface-variant uppercase">
                No active offers
              </p>
            </div>
          </Column>

          {/* Hired */}
          <Column title="Hired" count={1} hired>
            <div className="bg-surface-container-lowest border border-outline-variant p-4 corner-tick flex flex-col h-[280px]">
              <div className="flex justify-between items-start mb-2">
                <h4 className="font-pull-quote text-pull-quote text-primary !text-[24px] leading-tight">
                  Sarah Chen
                </h4>
                <div className="flex items-center gap-1 bg-sthira-forest/10 px-2 py-1 border border-sthira-forest/20 text-sthira-forest">
                  <Icon name="done_all" className="!text-[14px]" />
                </div>
              </div>
              <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mb-4">
                Offer Accepted: Jun 02
              </p>
              <div className="mt-auto pt-4 border-t border-outline-variant">
                <p className="font-tabular-data text-tabular-data text-on-surface-variant text-[13px] mb-3">
                  Pending structural provisioning and identity creation.
                </p>
                <button className="w-full bg-tamra-copper text-white py-3 font-mono-label text-mono-label uppercase hover:bg-tamra-copper-bright transition-colors flex items-center justify-center gap-2">
                  <Icon name="rocket_launch" className="!text-[18px]" />
                  Initiate Onboarding
                </button>
              </div>
            </div>
          </Column>
        </div>
      </div>
    </>
  )
}

function Column({
  title,
  count,
  hired,
  children,
}: {
  title: string
  count: number
  hired?: boolean
  children: React.ReactNode
}) {
  return (
    <div
      className={`w-[300px] flex-shrink-0 flex flex-col border border-outline-variant bg-surface-container-low overflow-hidden ${
        hired ? 'border-l-2 border-l-sthira-forest' : ''
      }`}
    >
      <div className="p-3 border-b border-outline-variant flex justify-between items-center bg-surface">
        <h3 className="font-mono-label text-mono-label uppercase text-primary flex items-center gap-2">
          {hired && <Icon name="check_circle" className="!text-[18px] text-sthira-forest" />}
          {title} <span className="text-on-surface-variant ml-1">({count})</span>
        </h3>
        <button className="text-on-surface-variant hover:text-primary">
          <Icon name="more_horiz" className="!text-[16px]" />
        </button>
      </div>
      <div className="flex-1 overflow-y-auto p-2 flex flex-col gap-2">{children}</div>
    </div>
  )
}

function Card({
  name,
  match,
  matchTone,
  body,
  footerLeft,
  footerRight,
}: {
  name: string
  match: string
  matchTone: string
  body: string
  footerLeft: string
  footerRight: React.ReactNode
}) {
  return (
    <div className="bg-surface-container-lowest border border-outline-variant p-4 cursor-pointer hover:border-yantra-indigo transition-colors corner-tick plate-container">
      <div className="flex justify-between items-start mb-4">
        <h4 className="font-pull-quote text-pull-quote text-primary !text-[24px] leading-tight">{name}</h4>
        <div className="flex items-center gap-1 bg-surface-container-high px-2 py-1 border border-outline-variant">
          <span className={`w-1.5 h-1.5 rounded-full ${matchTone}`} />
          <span className="font-tabular-data text-tabular-data text-[12px]">Match: {match}</span>
        </div>
      </div>
      <p className="font-body-standard text-body-standard text-on-surface-variant text-[13px] mb-4">{body}</p>
      <div className="flex justify-between items-center border-t border-outline-variant pt-3">
        <span className="font-mono-label text-mono-label text-on-surface-variant">{footerLeft}</span>
        {footerRight}
      </div>
    </div>
  )
}
