import { useEffect, useState } from 'react'
import { Icon } from '@/components/shell/Icon'
import type { PageClassification } from '@/lib/doctrine'
import { api, ApiError } from '@/lib/api'
import { ensureDevSession } from '@/lib/auth'

// Onboarding case ledger. PHASE 0 (live seam): real JWT + GET /onboarding/cases
// (pick the active case) + GET /onboarding/cases/{id} (tasks → integration timeline,
// documents → documentation ledger, progress → completion). Cultural induction and
// academic modules have no backend projection yet and remain static scaffolding.
export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'MGR',
  archetype: 'case',
  signals: 1,
  risks: 1,
  narratives: 1,
  maps: 1,
  bindu: true,
}

// ── Wire DTOs (Web JSON defaults: camelCase, enums as numbers, VO ids as { value }) ──

interface VoId { value: string }
interface CaseListItem {
  id: VoId
  employeeId: string
  employeeName: string
  caseType: number
  status: number
  expectedStartDate: string
  completedAt: string | null
  department: string | null
  role: string | null
  createdAt: string
}
interface TaskDto {
  id: VoId
  title: string
  category: number
  status: number
  assignedToEmployeeId: string | null
  dueDate: string | null
  completedAt: string | null
  completionNotes: string | null
  skipReason: string | null
  isOverdue: boolean
}
interface DocDto {
  id: VoId
  documentType: number
  status: number
  fileName: string | null
  submittedAt: string | null
  verifiedAt: string | null
  rejectionReason: string | null
}
interface CaseDetail extends CaseListItem {
  progressPercent: number
  tasks: TaskDto[]
  documents: DocDto[]
}

// ── Enum decode tables (mirror Karamchari.Onboarding.Domain.OnboardingPrimitives) ──

const CASE_STATUS: Record<number, string> = { 1: 'Pending', 2: 'In Progress', 3: 'Completed', 4: 'Cancelled' }
const TASK_DONE = 3
const TASK_SKIPPED = 4

const TASK_CATEGORY: Record<number, { label: string; icon: string }> = {
  1: { label: 'HR', icon: 'badge' },
  2: { label: 'IT', icon: 'laptop_mac' },
  3: { label: 'Manager', icon: 'supervisor_account' },
  4: { label: 'Facilities', icon: 'meeting_room' },
  5: { label: 'Finance', icon: 'account_balance' },
  6: { label: 'Legal', icon: 'gavel' },
}

const DOC_META: Record<number, { name: string; icon: string }> = {
  1: { name: 'Government ID Proof', icon: 'badge' },
  2: { name: 'Address Proof', icon: 'home' },
  3: { name: 'Payroll Bank Information', icon: 'account_balance' },
  4: { name: 'Tax Declaration', icon: 'receipt_long' },
  5: { name: 'Education Certificate', icon: 'school' },
  6: { name: 'Relieving Letter', icon: 'work_history' },
  7: { name: 'Employment Contract', icon: 'contract' },
  8: { name: 'Non-Disclosure Agreement', icon: 'contract' },
  9: { name: 'Medical Fitness Certificate', icon: 'health_and_safety' },
}

// Document status → ledger row state. 1 Pending, 2 Submitted, 3 Verified, 4 Rejected.
type DocState = 'pending' | 'review' | 'verified' | 'rejected'
function docState(status: number): DocState {
  return status === 3 ? 'verified' : status === 2 ? 'review' : status === 4 ? 'rejected' : 'pending'
}

function idFromHash(): string | null {
  const q = window.location.hash.split('?')[1]
  return q ? new URLSearchParams(q).get('id') : null
}

function firstName(full: string): string {
  return full.split(/\s+/)[0] || full
}

function fmtDate(iso: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  return d.toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: '2-digit' }).replace(/ /g, '-')
}

export function OnboardingCase() {
  const [detail, setDetail] = useState<CaseDetail | null>(null)
  const [state, setState] = useState<'loading' | 'ready' | 'error' | 'empty'>('loading')
  const [error, setError] = useState('')

  useEffect(() => {
    let alive = true
    ;(async () => {
      try {
        setState('loading')
        await ensureDevSession()

        let id = idFromHash()
        if (!id) {
          const list = await api.get<CaseListItem[]>('/api/v1/onboarding/cases')
          if (!list.length) { if (alive) setState('empty'); return }
          // Prefer an in-progress case; otherwise the most recent.
          const active = list.find((c) => c.status === 2) ?? list[0]
          id = active.id.value
        }

        const d = await api.get<CaseDetail>(`/api/v1/onboarding/cases/${id}`)
        if (!alive) return
        setDetail(d)
        setState('ready')
      } catch (e) {
        if (!alive) return
        setError(e instanceof ApiError ? `API ${e.status} ${e.statusText}` : String(e))
        setState('error')
      }
    })()
    return () => { alive = false }
  }, [])

  if (state === 'loading') return <CenterNote text="Loading onboarding case…" />
  if (state === 'empty') return <CenterNote text="No onboarding cases registered for this tenant." />
  if (state === 'error') return <CenterNote text={`Could not load onboarding case. ${error}`} tone="error" />
  if (!detail) return null

  const tasks = detail.tasks
  const firstOpen = tasks.findIndex((t) => t.status !== TASK_DONE && t.status !== TASK_SKIPPED)
  const pendingDocs = detail.documents.filter((d) => docState(d.status) === 'pending' || docState(d.status) === 'rejected')
  const verifiedDocs = detail.documents.filter((d) => docState(d.status) === 'verified')
  const caseCode = `OB // ${detail.id.value.replace(/-/g, '').slice(0, 8).toUpperCase()}`

  return (
    <>
      {/* Case header */}
      <div className="pb-8 hairline-b mb-12">
        <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
          <div>
            <span className="font-mono-label text-mono-label text-on-surface-variant tracking-widest uppercase mb-4 block">
              Case Ledger // {caseCode}
            </span>
            <h2 className="font-section-title text-section-title !text-4xl md:!text-section-title text-primary tracking-tight">
              Namaskaram, {firstName(detail.employeeName)}.
            </h2>
            <p className="font-body-standard text-body-standard text-on-surface-variant mt-2 max-w-2xl">
              Onboarding procedural flow{detail.role ? ` for ${detail.role}` : ''}
              {detail.department ? ` · ${detail.department}` : ''}. Status: {CASE_STATUS[detail.status] ?? detail.status}.
            </p>
          </div>
          <div className="flex items-center gap-3">
            <button className="px-[22px] py-[14px] bg-transparent border border-outline-variant text-primary font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-low transition-colors">
              Halt Process
            </button>
            <button className="px-[22px] py-[14px] bg-tamra-copper text-white font-mono-label text-mono-label uppercase tracking-widest hover:bg-tamra-copper-bright transition-colors">
              Approve Next Phase
            </button>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-gutter">
        {/* Left: timeline + ledger */}
        <div className="lg:col-span-8 flex flex-col gap-12">
          <section>
            <div className="mb-6 flex justify-between items-baseline">
              <h3 className="font-mono-label text-mono-label text-primary uppercase tracking-widest border-b border-primary inline-block pb-1">
                Integration Timeline
              </h3>
              <span className="font-tabular-data text-tabular-data text-on-surface-variant">
                {detail.progressPercent}% Complete
              </span>
            </div>
            <div className="bg-surface-container-lowest hairline-all p-8 relative overflow-x-auto">
              <div className="relative min-w-[600px]">
                <div className="absolute top-4 left-12 right-12 h-[1px] bg-outline-variant z-0" />
                <div
                  className="absolute top-4 left-12 h-[2px] bg-tamra-copper z-0"
                  style={{ width: `calc((100% - 6rem) * ${detail.progressPercent} / 100)` }}
                />
                <div className="relative z-10 flex justify-between">
                {tasks.map((t, i) => {
                  const done = t.status === TASK_DONE
                  const active = i === firstOpen
                  const cat = TASK_CATEGORY[t.category] ?? { label: '—', icon: 'pending' }
                  return (
                    <div key={t.id.value} className="flex flex-col items-center gap-3 w-24 shrink-0">
                      <div
                        className={
                          active
                            ? 'w-8 h-8 rounded-full bg-tamra-copper border-2 border-tamra-copper flex items-center justify-center text-white shadow-[0_0_0_4px_rgba(177,106,60,0.1)]'
                            : done
                              ? 'w-8 h-8 rounded-full bg-surface border-2 border-tamra-copper flex items-center justify-center text-tamra-copper'
                              : 'w-8 h-8 rounded-full bg-surface-container-lowest border-2 border-outline-variant flex items-center justify-center text-on-surface-variant'
                        }
                      >
                        <Icon name={done ? 'check' : cat.icon} className="!text-[16px]" />
                      </div>
                      <span
                        className={`font-mono-label text-mono-label text-center leading-tight ${
                          active ? 'text-primary font-bold' : done ? 'text-primary' : 'text-on-surface-variant'
                        }`}
                      >
                        {t.title}
                      </span>
                    </div>
                  )
                })}
                </div>
              </div>
            </div>
          </section>

          <section>
            <div className="mb-6 flex justify-between items-baseline">
              <h3 className="font-mono-label text-mono-label text-primary uppercase tracking-widest border-b border-primary inline-block pb-1">
                Documentation Ledger
              </h3>
              <div className="flex gap-4">
                <span className="font-mono-label text-mono-label text-rakta-critical flex items-center gap-1">
                  <span className="bindu rakta-critical" /> {pendingDocs.length} Pending
                </span>
                <span className="font-mono-label text-mono-label text-sthira-forest flex items-center gap-1">
                  <span className="bindu good" /> {verifiedDocs.length} Verified
                </span>
              </div>
            </div>
            <div className="bg-surface-container-lowest hairline-all flex flex-col">
              <div className="grid grid-cols-12 gap-4 px-6 py-3 border-b border-outline-variant bg-surface-container-low">
                <div className="col-span-1 font-mono-label text-mono-label text-on-surface-variant">STAT</div>
                <div className="col-span-5 font-mono-label text-mono-label text-on-surface-variant">REQUIREMENT</div>
                <div className="col-span-3 font-mono-label text-mono-label text-on-surface-variant">OWNER</div>
                <div className="col-span-3 font-mono-label text-mono-label text-on-surface-variant text-right">ACTION</div>
              </div>
              {detail.documents.length === 0 && (
                <div className="px-6 py-6 font-tabular-data text-tabular-data text-on-surface-variant">
                  No documents required for this case.
                </div>
              )}
              {detail.documents.map((doc, i) => {
                const st = docState(doc.status)
                const isLast = i === detail.documents.length - 1
                const meta = DOC_META[doc.documentType] ?? { name: `Document #${doc.documentType}`, icon: 'description' }
                const flagged = st === 'pending' || st === 'rejected'
                return (
                  <div
                    key={doc.id.value}
                    className={`grid grid-cols-12 gap-4 px-6 py-4 items-center transition-colors ${
                      isLast ? '' : 'border-b border-outline-variant'
                    } ${flagged ? 'bg-surface-container-low border-l-2 border-l-rakta-critical' : 'hover:bg-surface'}`}
                  >
                    <div className="col-span-1 flex items-center pl-1">
                      <span className={`bindu ${flagged ? 'rakta-critical animate-pulse' : 'good'}`} />
                    </div>
                    <div className="col-span-5 flex items-center gap-3">
                      <Icon name={meta.icon} className={flagged ? 'text-rakta-critical' : 'text-on-surface-variant'} />
                      <span className={`font-body-standard text-body-standard text-primary ${flagged ? 'font-bold' : ''}`}>
                        {meta.name}
                      </span>
                    </div>
                    <div className="col-span-3 font-tabular-data text-tabular-data text-on-surface-variant">EMP</div>
                    <div className="col-span-3 text-right">
                      {flagged ? (
                        <button className="px-[12px] py-[6px] bg-rakta-critical text-white font-mono-label text-mono-label">
                          {st === 'rejected' ? 'Re-request' : 'Issue Nudge'}
                        </button>
                      ) : st === 'verified' ? (
                        <a className="text-yantra-indigo font-mono-label text-mono-label hover:underline cursor-pointer">
                          View Artifact
                        </a>
                      ) : (
                        <span className="font-tabular-data text-tabular-data text-on-surface-variant">
                          {fmtDate(doc.submittedAt)} · review
                        </span>
                      )}
                    </div>
                  </div>
                )
              })}
            </div>
          </section>
        </div>

        {/* Right: culture + training (static scaffolding — no backend projection yet) */}
        <div className="lg:col-span-4 flex flex-col gap-12">
          <section>
            <div className="mb-6">
              <h3 className="font-mono-label text-mono-label text-primary uppercase tracking-widest border-b border-primary inline-block pb-1">
                Cultural Induction
              </h3>
            </div>
            <div className="bg-ivory-2 hairline-all p-6 relative corner-tick">
              <div className="flex gap-4 items-start mb-4">
                <div className="w-12 h-12 bg-surface-variant shrink-0 border border-outline-variant flex items-center justify-center">
                  <span className="font-serif font-light text-lg text-graphite">
                    {firstName(detail.employeeName).slice(0, 1)}
                  </span>
                </div>
                <div>
                  <p className="font-mono-label text-mono-label text-on-surface-variant mb-1">From: Reporting Manager</p>
                  <p className="font-body-standard text-body-standard text-primary italic">
                    "A structured first thirty days sets the tone for the whole tenure. The institution
                    welcomes {firstName(detail.employeeName)} and is preparing the ground for their
                    contribution."
                  </p>
                </div>
              </div>
              <button className="w-full py-[9px] bg-transparent border border-outline-variant text-primary font-mono-label text-mono-label uppercase tracking-widest hover:bg-surface-container-lowest transition-colors mt-2">
                Add Welcome Note
              </button>
            </div>
          </section>

          <section>
            <div className="mb-6">
              <h3 className="font-mono-label text-mono-label text-primary uppercase tracking-widest border-b border-primary inline-block pb-1">
                Academic Modules
              </h3>
            </div>
            <div className="flex flex-col gap-3">
              <a className="group block bg-surface-container-lowest hairline-all p-4 hover:border-yantra-indigo hover:bg-surface transition-all cursor-pointer">
                <div className="flex justify-between items-start mb-2">
                  <span className="font-mono-label text-mono-label text-tamra-copper bg-surface-container-low px-2 py-1">
                    Mandatory
                  </span>
                  <Icon name="arrow_outward" className="text-outline group-hover:text-yantra-indigo transition-colors" />
                </div>
                <h4 className="font-body-standard text-body-standard text-primary font-bold mb-1">
                  Information Security Level 1
                </h4>
                <p className="font-tabular-data text-tabular-data text-on-surface-variant">
                  Est. time: 45 mins • Due Day 3
                </p>
              </a>
              <a className="group block bg-surface-container-lowest hairline-all p-4 hover:border-yantra-indigo hover:bg-surface transition-all cursor-pointer">
                <div className="flex justify-between items-start mb-2">
                  <span className="font-mono-label text-mono-label text-on-surface-variant bg-surface-container-high px-2 py-1">
                    Recommended
                  </span>
                  <Icon name="arrow_outward" className="text-outline group-hover:text-yantra-indigo transition-colors" />
                </div>
                <h4 className="font-body-standard text-body-standard text-primary font-bold mb-1">
                  Architecture Principles
                </h4>
                <p className="font-tabular-data text-tabular-data text-on-surface-variant">
                  Est. time: 120 mins • Ongoing
                </p>
              </a>
            </div>
          </section>
        </div>
      </div>
    </>
  )
}

function CenterNote({ text, tone = 'muted' }: { text: string; tone?: 'muted' | 'error' }) {
  return (
    <div className="flex items-center justify-center py-32">
      <span
        className={`font-mono-label text-mono-label uppercase tracking-widest ${
          tone === 'error' ? 'text-rakta-critical' : 'text-on-surface-variant'
        }`}
      >
        {text}
      </span>
    </div>
  )
}
