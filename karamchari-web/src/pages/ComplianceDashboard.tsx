import { PageHeader } from '@/components/shell/PageHeader'
import { Icon } from '@/components/shell/Icon'
import type { PageClassification } from '@/lib/doctrine'

export const PAGE_CLASSIFICATION: PageClassification = {
  persona: 'CMP',
  archetype: 'dashboard',
  signals: 1,
  risks: 1,
  narratives: 0,
  maps: 1,
  bindu: true,
}

const violations = [
  { id: '#VL-892', breach: 'Data Residency Gap (EU-04)', entity: 'Node_Frankfurt_02', severity: 'Critical', tone: 'bg-rakta-critical', action: 'text-primary hover:text-graphite' },
  { id: '#VL-891', breach: 'Access Log Obfuscation', entity: 'Service_Acct_Admin', severity: 'Elevated', tone: 'bg-pita-caution', action: 'text-primary hover:text-tamra-copper' },
  { id: '#VL-889', breach: 'Missed Training Mandate', entity: 'Dept_Engineering', severity: 'Monitored', tone: 'bg-sthira-forest', action: 'text-primary hover:text-tamra-copper' },
]

const registers = [
  { icon: 'verified_user', tone: 'text-sthira-forest', name: 'GDPR (EU)', note: 'Full compliance verified. Next audit: Q3.' },
  { icon: 'policy', tone: 'text-pita-caution', name: 'CCPA (US-CA)', note: 'Pending minor updates to consent flow.' },
  { icon: 'fact_check', tone: 'text-sthira-forest', name: 'SOC2 Type II', note: 'Continuous monitoring active.' },
  { icon: 'gavel', tone: 'text-rakta-critical', name: 'HIPAA (US)', note: 'BAA missing for Vendor_X_Tech.' },
]

const auditTrail = [
  { ts: '2026-06-11T14:32:01Z', tag: 'ACT_READ', tagTone: '', line: 'border-outline-variant', dot: 'bg-outline-variant', body: <>User <Code>usr_98x2</Code> accessed restricted table <Code>tbl_financials_q3</Code> via API key.</> },
  { ts: '2026-06-11T12:15:44Z', tag: 'POL_PASS', tagTone: 'text-sthira-forest', line: 'border-sthira-forest', dot: 'bg-sthira-forest', body: <>Automated GDPR compliance sweep completed. 0 anomalies detected in dataset <Code>ds_eu_customers</Code>.</> },
  { ts: '2026-06-11T09:02:11Z', tag: 'SYS_WARN', tagTone: 'text-rakta-critical', line: 'border-rakta-critical', dot: 'bg-rakta-critical', body: <>Multiple failed authentication attempts (5) from unrecognized IP <Code>192.168.1.104</Code> targeting admin gateway.</> },
  { ts: '2026-06-10T18:45:00Z', tag: 'ACT_MOD', tagTone: '', line: 'border-outline-variant', dot: 'bg-outline-variant', body: <>Configuration changed for resource <Code>s3_bucket_logs</Code>. Encryption mandated by <Code>sys_admin_01</Code>.</> },
]

function Code({ children }: { children: React.ReactNode }) {
  return (
    <span className="font-mono text-[11px] bg-surface px-1 border border-outline-variant mx-1">{children}</span>
  )
}

export function ComplianceDashboard() {
  return (
    <>
      <PageHeader
        kicker="Real-time Regulatory Posture & Audit Surface"
        title="Compliance Matrix"
        right={
          <div className="flex gap-3">
            <button className="px-5 py-2.5 bg-transparent border border-outline-variant text-primary font-mono text-[11px] tracking-wider uppercase hover:bg-surface-container-low transition-colors">
              Export Ledger
            </button>
            <button className="px-5 py-2.5 bg-tamra-copper text-white font-mono text-[11px] tracking-wider uppercase hover:bg-tamra-copper-bright transition-colors">
              Run Deep Scan
            </button>
          </div>
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 auto-rows-min">
        {/* Global Compliance Score */}
        <div className="lg:col-span-4 bg-ivory-2 border border-outline-variant p-6 relative flex flex-col justify-between min-h-[240px] corner-tick">
          <div className="flex justify-between items-start">
            <span className="font-mono-label text-mono-label text-ink uppercase tracking-widest">
              Global Posture
            </span>
            <span className="bindu good" />
          </div>
          <div className="mt-8">
            <div className="flex items-baseline gap-2">
              <span className="font-serif font-light text-[64px] leading-none text-ink tracking-tight">98.4</span>
              <span className="font-mono text-[14px] text-graphite">%</span>
            </div>
            <p className="font-body-standard text-[13px] text-graphite mt-2 border-t border-ink/10 pt-2">
              Systemic alignment verified across 47 active jurisdictions. No critical deviations detected in
              current cycle.
            </p>
          </div>
        </div>

        {/* Violation Queue */}
        <div className="lg:col-span-8 bg-surface-container-lowest border border-outline-variant p-0 flex flex-col relative">
          <div className="p-4 border-b border-outline-variant bg-surface flex justify-between items-center">
            <span className="font-mono-label text-mono-label text-primary uppercase tracking-widest">
              Violation Queue [Active]
            </span>
            <span className="px-2 py-1 bg-surface-container-high border border-outline-variant text-[10px] font-mono tracking-wider">
              3 Items
            </span>
          </div>
          <div className="flex-1 overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-outline-variant bg-surface-container-low">
                  {['ID', 'Policy Breach', 'Entity', 'Severity', 'Action'].map((h, i) => (
                    <th
                      key={h}
                      className={`py-3 px-4 font-mono text-[10px] text-on-surface-variant uppercase tracking-wider font-normal ${i === 4 ? 'text-right' : ''}`}
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="font-tabular-data text-tabular-data text-on-surface">
                {violations.map((v, i) => (
                  <tr
                    key={v.id}
                    className={`hover:bg-surface-container-low transition-colors ${i < violations.length - 1 ? 'border-b border-outline-variant' : ''}`}
                  >
                    <td className="py-3 px-4 text-on-surface-variant">{v.id}</td>
                    <td className="py-3 px-4">{v.breach}</td>
                    <td className="py-3 px-4">{v.entity}</td>
                    <td className="py-3 px-4">
                      <div className="flex items-center gap-2">
                        <span className={`w-1.5 h-1.5 rounded-full ${v.tone}`} />
                        <span className="text-[12px]">{v.severity}</span>
                      </div>
                    </td>
                    <td className="py-3 px-4 text-right">
                      <button className={`text-[11px] font-mono uppercase tracking-wider ${v.action}`}>
                        Audit
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        {/* Regulatory Register */}
        <div className="lg:col-span-5 bg-surface-container-lowest border border-outline-variant flex flex-col">
          <div className="p-4 border-b border-outline-variant bg-surface">
            <span className="font-mono-label text-mono-label text-primary uppercase tracking-widest">
              Regulatory Register
            </span>
          </div>
          <div className="p-0 flex-1 overflow-y-auto max-h-[300px]">
            <ul className="divide-y divide-outline-variant">
              {registers.map((r) => (
                <li
                  key={r.name}
                  className="p-4 flex items-center justify-between hover:bg-surface-container-low transition-colors group cursor-pointer"
                >
                  <div className="flex items-center gap-3">
                    <Icon name={r.icon} className={`!text-[18px] ${r.tone}`} />
                    <div>
                      <div className="font-mono text-[11px] text-primary tracking-wider">{r.name}</div>
                      <div className="text-[12px] text-on-surface-variant mt-0.5">{r.note}</div>
                    </div>
                  </div>
                  <Icon
                    name="chevron_right"
                    className="!text-[16px] text-outline opacity-0 group-hover:opacity-100 transition-opacity"
                  />
                </li>
              ))}
            </ul>
          </div>
        </div>

        {/* Audit Trail Explorer */}
        <div className="lg:col-span-7 bg-surface-container-lowest border border-outline-variant p-6 relative">
          <div className="flex justify-between items-center mb-6">
            <span className="font-mono-label text-mono-label text-primary uppercase tracking-widest">
              Audit Trail Explorer
            </span>
            <div className="flex gap-2">
              <input
                className="px-3 py-1.5 bg-surface border border-outline-variant rounded-none text-[12px] font-tabular-data focus:ring-1 focus:ring-primary focus:border-primary focus:outline-none w-32 placeholder:text-on-surface-variant"
                placeholder="Entity ID..."
                type="text"
              />
              <button className="px-3 py-1.5 bg-surface-container-high border border-outline-variant text-primary font-mono text-[10px] uppercase hover:bg-surface-dim transition-colors flex items-center gap-1">
                <Icon name="filter_list" className="!text-[14px]" />
                Filter
              </button>
            </div>
          </div>
          <div className="space-y-4">
            {auditTrail.map((e) => (
              <div key={e.ts} className={`pl-4 border-l-2 ${e.line} relative`}>
                <div className={`absolute w-2 h-2 ${e.dot} rounded-full -left-[5px] top-1`} />
                <div className="flex justify-between items-baseline mb-1">
                  <span className="font-mono text-[10px] text-on-surface-variant">{e.ts}</span>
                  <span
                    className={`font-mono text-[9px] bg-surface-container-high px-1.5 py-0.5 border border-outline-variant ${e.tagTone}`}
                  >
                    {e.tag}
                  </span>
                </div>
                <p className="font-tabular-data text-[13px] text-primary">{e.body}</p>
              </div>
            ))}
          </div>
          <button className="w-full mt-6 py-2 border border-outline-variant text-center font-mono text-[10px] uppercase tracking-widest text-on-surface-variant hover:bg-surface-container-low hover:text-primary transition-colors">
            Load Older Entries
          </button>
        </div>
      </div>
    </>
  )
}
