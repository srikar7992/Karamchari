import { SignalCard, NarrativePlate, InstitutionalTable } from '@/components/institutional'
import type { TableColumn } from '@/components/institutional'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'EMP',
  archetype: 'record-detail',
  signals: 3,
  risks: 0,
  narratives: 1,
  maps: 0,
  bindu: false,
}

// --- Data ---

const PROFILE = {
  name: 'Asha Venkatesan',
  number: 'EMP-88271',
  email: 'asha.v@dev.local',
  designation: 'Senior Engineer',
  department: 'Zone Operations',
  branch: 'Hyderabad East',
  hired: '2021-03-15',
  contract: 'Permanent',
  manager: 'Ananya Rajesh',
  timezone: 'Asia/Kolkata',
}

interface ContactField { k: string; v: string }
const CONTACT: ContactField[] = [
  { k: 'Work Email', v: 'asha.v@dev.local' },
  { k: 'Personal Email', v: 'asha.venkatesan@gmail.com' },
  { k: 'Mobile', v: '+91 98765 43210' },
  { k: 'City', v: 'Hyderabad, Telangana' },
]

interface EmergencyContact { name: string; relation: string; phone: string; priority: string }
const EMERGENCY: EmergencyContact[] = [
  { name: 'Ravi Venkatesan', relation: 'Spouse', phone: '+91 98765 11111', priority: 'Primary' },
  { name: 'Priya Venkatesan', relation: 'Parent', phone: '+91 98765 22222', priority: 'Secondary' },
]

interface Doc { name: string; type: string; uploaded: string; status: string }
const DOCUMENTS: Doc[] = [
  { name: 'Appointment Letter', type: 'Employment', uploaded: '2021-03-15', status: 'Verified' },
  { name: 'PAN Card',           type: 'Identity',   uploaded: '2021-03-15', status: 'Verified' },
  { name: 'Aadhaar Card',       type: 'Identity',   uploaded: '2021-03-15', status: 'Verified' },
  { name: 'Degree Certificate', type: 'Qualification', uploaded: '2021-04-01', status: 'Verified' },
]

// --- Helpers ---

function tenure(hired: string): string {
  const ms = Date.now() - new Date(hired).getTime()
  const years = Math.floor(ms / (365.25 * 24 * 3600 * 1000))
  const months = Math.floor((ms % (365.25 * 24 * 3600 * 1000)) / (30.44 * 24 * 3600 * 1000))
  return `${years}y ${months}m`
}

// --- Page ---

export function MyProfile() {
  const emergencyCols: TableColumn<EmergencyContact>[] = [
    { key: 'name',     header: 'Name' },
    { key: 'relation', header: 'Relation' },
    { key: 'phone',    header: 'Phone' },
    { key: 'priority', header: 'Priority' },
  ]
  const docCols: TableColumn<Doc>[] = [
    { key: 'name',     header: 'Document' },
    { key: 'type',     header: 'Type' },
    { key: 'uploaded', header: 'Uploaded' },
    {
      key: 'status',
      header: 'Status',
      render: (r) => (
        <span className={`font-mono-label text-mono-label uppercase ${r.status === 'Verified' ? 'text-sthira-forest' : 'text-pita-caution'}`}>
          {r.status}
        </span>
      ),
    },
  ]

  return (
    <>
      {/* Identity header */}
      <header className="mb-10 border-b border-outline-variant pb-8">
        <div className="flex items-start gap-8">
          <div className="w-16 h-16 bg-surface-container-low hairline-all flex items-center justify-center shrink-0">
            <span className="font-serif font-light text-[28px] text-primary">
              {PROFILE.name.split(' ').map((n) => n[0]).join('')}
            </span>
          </div>
          <div className="flex-1 min-w-0">
            <h2 className="font-serif font-light text-[40px] leading-[1.1] text-primary">
              {PROFILE.name}
            </h2>
            <p className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mt-2">
              {PROFILE.number} · {PROFILE.designation} · {PROFILE.department}
            </p>
          </div>
          <div className="flex items-center gap-2 hairline-all px-3 py-2 bg-surface-container-lowest shrink-0">
            <span className="bindu good" />
            <span className="font-mono-label text-mono-label text-sthira-forest uppercase">Active</span>
          </div>
        </div>
      </header>

      {/* Signals */}
      <section className="mb-10">
        <div className="grid grid-cols-2 sm:grid-cols-2 md:grid-cols-3 gap-grid-12">
          <SignalCard label="Tenure" value={tenure(PROFILE.hired)} note={`Joined ${PROFILE.hired}`} cornerTick />
          <SignalCard label="Contract Type" value={PROFILE.contract} note={`Branch: ${PROFILE.branch}`} />
          <SignalCard label="Documents Filed" value={String(DOCUMENTS.length)} unit="records" note="All verified" bindu="good" />
        </div>
      </section>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-grid-12">
        <div className="lg:col-span-2 space-y-10">
          {/* Employment */}
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Employment
            </h3>
            <div className="hairline-all bg-surface-container-lowest">
              {([
                { k: 'Employee Number', v: PROFILE.number },
                { k: 'Designation',     v: PROFILE.designation },
                { k: 'Department',      v: PROFILE.department },
                { k: 'Branch',          v: PROFILE.branch },
                { k: 'Reports To',      v: PROFILE.manager },
                { k: 'Date of Joining', v: PROFILE.hired },
                { k: 'Timezone',        v: PROFILE.timezone },
              ] as ContactField[]).map((f, i, arr) => (
                <div key={f.k} className={`flex justify-between px-6 py-4 ${i < arr.length - 1 ? 'border-b border-outline-variant' : ''}`}>
                  <span className="font-mono-label text-mono-label text-on-surface-variant uppercase">{f.k}</span>
                  <span className="font-tabular-data text-tabular-data text-primary">{f.v}</span>
                </div>
              ))}
            </div>
          </section>

          {/* Contact */}
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Contact
            </h3>
            <div className="hairline-all bg-surface-container-lowest">
              {CONTACT.map((f, i) => (
                <div key={f.k} className={`flex justify-between px-6 py-4 ${i < CONTACT.length - 1 ? 'border-b border-outline-variant' : ''}`}>
                  <span className="font-mono-label text-mono-label text-on-surface-variant uppercase">{f.k}</span>
                  <span className="font-tabular-data text-tabular-data text-primary">{f.v}</span>
                </div>
              ))}
            </div>
          </section>

          {/* Emergency Contacts */}
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Emergency Contacts
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={emergencyCols} rows={EMERGENCY} rowKey={(r) => r.name} />
            </div>
          </section>

          {/* Documents */}
          <section>
            <h3 className="font-mono-label text-mono-label text-on-surface-variant uppercase tracking-widest mb-6">
              Documents
            </h3>
            <div className="overflow-x-auto hairline-all bg-surface-container-lowest p-6">
              <InstitutionalTable columns={docCols} rows={DOCUMENTS} rowKey={(r) => r.name} />
            </div>
          </section>
        </div>

        <div>
          <NarrativePlate kicker="Employment Record" source="HR-RECORD-01">
            Asha Venkatesan has maintained continuous service since March 2021. This profile
            is the institutional record of that relationship — the authority, the terms, and
            the people behind it. Changes flow through HR, not this surface.
          </NarrativePlate>
        </div>
      </div>
    </>
  )
}
