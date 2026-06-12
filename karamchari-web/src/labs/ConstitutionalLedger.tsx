// LAB EXPERIMENT A — Employee 360 as Constitutional Ledger.
// Not production. Outside doctrine jurisdiction (design/EXPERIMENTS.md).
// Question: what if an employee record were not a profile with tabs but a
// continuous constitutional record — git history + court record + biography?

interface Act {
  ref: string
  hash: string
  date: string
  article: string
  title: string
  body: string
  actor: string
  authority: string
  tone?: 'copper' | 'forest' | 'neutral'
}

const ARTICLES = [
  { numeral: 'I', name: 'Identity' },
  { numeral: 'II', name: 'Assignments' },
  { numeral: 'III', name: 'Capability' },
  { numeral: 'IV', name: 'Recognition' },
  { numeral: 'V', name: 'Compensation' },
  { numeral: 'VI', name: 'Authority Actions' },
]

const ACTS: Act[] = [
  {
    ref: 'ACT-0001', hash: 'a3f29c1', date: '2022-02-14', article: 'I', title: 'Identity Constituted',
    body: 'Employee identity EMP-8834-A created from accepted offer OFR-2022-031 under HIRE-ID-01. Elena Rostova enters the institution as Technical Architect, Core Infrastructure. Onboarding case ONB-2022-114 opened the same hour.',
    actor: 'M. Vance', authority: 'HIRE-ID-01', tone: 'copper',
  },
  {
    ref: 'ACT-0007', hash: 'c81d04e', date: '2022-02-25', article: 'I', title: 'Onboarding Sealed',
    body: 'All identity documents verified; assets issued (laptop, token, badge). Onboarding case closed in eleven days against a fourteen-day standard. The identity is complete and active.',
    actor: 'Onboarding Desk', authority: 'ONB-STD-02',
  },
  {
    ref: 'ACT-0019', hash: 'f4290aa', date: '2024-03-01', article: 'II', title: 'Position Assignment',
    body: 'Assigned Senior Technical Architect, Core Infrastructure, reporting to VP Infrastructure Operations. Span of authority: design review board seat, three direct reports. Prior position closed, not overwritten.',
    actor: 'M. Vance', authority: 'ORG-POS-03',
  },
  {
    ref: 'ACT-0026', hash: '0b77d13', date: '2025-08-11', article: 'III', title: 'Capability Validated at Mastery',
    body: 'Distributed Systems Design validated at level five. Evidence: migration design review board, four-session record. Capability claims in this record are validated acts, never self-declared attributes.',
    actor: 'Review Board', authority: 'CAP-VAL-01', tone: 'forest',
  },
  {
    ref: 'ACT-0031', hash: '9e3c5b8', date: '2026-01-19', article: 'IV', title: 'Institutional Recognition Conferred',
    body: 'Q1 Infrastructure Excellence Award conferred by the CTO for the database migration protocol. Recognition is a constitutional act: it cites its evidence and joins the permanent record.',
    actor: 'CTO Office', authority: 'RCG-STD-01', tone: 'copper',
  },
  {
    ref: 'ACT-0038', hash: '5d10f72', date: '2026-04-02', article: 'V', title: 'Compensation Revised',
    body: 'Annual revision applied: base CHF 178,000 to CHF 185,000, compa-ratio 1.12 against Tier 4 band. The prior figure remains readable in the record; amendment is a new act, never an edit.',
    actor: 'M. Vance', authority: 'CMP-REV-02',
  },
  {
    ref: 'ACT-0047', hash: 'e2a8c90', date: '2026-06-11', article: 'VI', title: 'Retention Mandate Under Consideration',
    body: 'Buddhi flags elevated flight risk: role tenure stalled at eleven months of unchanged scope while market velocity for senior architects rises. A strategic mandate within Q3 retains this capability at materially lower cost than its replacement. The decision, when taken, will be the forty-eighth act of this record.',
    actor: 'Buddhi · Ananya R.', authority: 'RET-ACT-01 (pending)', tone: 'neutral',
  },
]

const TONE_DOT: Record<string, string> = {
  copper: 'bg-tamra-copper',
  forest: 'bg-sthira-forest',
  neutral: 'bg-outline',
}

export function ConstitutionalLedger() {
  return (
    <div className="bg-ivory min-h-screen text-ink">
      <div className="max-w-[960px] mx-auto px-6 md:px-12 py-16">
        {/* Frontispiece */}
        <header className="mb-20">
          <p className="font-mono text-[10px] uppercase tracking-[0.25em] text-on-surface-variant mb-8">
            Karamchari · Constitutional Record · Volume EMP-8834-A
          </p>
          <h1 className="font-serif font-light text-[64px] md:text-[84px] leading-[0.95] tracking-tight mb-6">
            Elena Rostova
          </h1>
          <div className="flex flex-wrap gap-x-10 gap-y-2 font-mono text-[11px] uppercase tracking-widest text-on-surface-variant border-y border-ink/20 py-4">
            <span>47 acts since 2022-02-14</span>
            <span>6 articles</span>
            <span>Every act cites actor and authority</span>
            <span className="text-ink">Nothing is ever overwritten</span>
          </div>
        </header>

        <div className="grid grid-cols-[48px_1fr] md:grid-cols-[120px_1fr] gap-x-6 md:gap-x-12">
          {/* Article rail */}
          <nav className="row-span-full pt-2 sticky top-8 self-start hidden md:block">
            {ARTICLES.map((a) => (
              <div key={a.numeral} className="mb-8">
                <div className="font-serif font-light text-[28px] leading-none text-ink/30">{a.numeral}</div>
                <div className="font-mono text-[9px] uppercase tracking-[0.2em] text-on-surface-variant mt-1">{a.name}</div>
              </div>
            ))}
          </nav>

          {/* The ledger spine */}
          <main className="relative border-l-2 border-ink/70 pl-8 md:pl-14">
            {ACTS.map((act, i) => {
              const article = ARTICLES.find((a) => a.numeral === act.article)
              const newArticle = i === 0 || ACTS[i - 1].article !== act.article
              return (
                <article key={act.ref} className="relative mb-16">
                  {/* spine node */}
                  <span
                    className={`absolute -left-[39px] md:-left-[63px] top-2 w-3 h-3 rounded-full ${TONE_DOT[act.tone ?? 'neutral']} ring-4 ring-ivory`}
                  />
                  {newArticle && (
                    <div className="mb-6 -mt-2">
                      <span className="font-serif font-light italic text-[22px] text-ink/60">
                        Article {act.article} — {article?.name}
                      </span>
                    </div>
                  )}
                  <div className="flex flex-wrap items-baseline gap-x-5 gap-y-1 mb-3 font-mono text-[10.5px] uppercase tracking-widest text-on-surface-variant">
                    <span className="text-ink font-medium">{act.ref}</span>
                    <span className="lowercase tracking-normal text-tamra-copper">{act.hash}</span>
                    <span>{act.date}</span>
                    <span>actor {act.actor}</span>
                    <span>authority {act.authority}</span>
                  </div>
                  <h2 className="font-serif font-light text-[30px] leading-tight tracking-tight mb-3">
                    {act.title}
                  </h2>
                  <p className="font-sans text-[15px] leading-[1.65] text-ink/80 max-w-[58ch]">
                    {act.body}
                  </p>
                </article>
              )
            })}

            {/* Open end of the record */}
            <div className="relative pb-4">
              <span className="absolute -left-[36px] md:-left-[60px] top-1 w-[10px] h-[10px] border-2 border-ink/50 rounded-full bg-ivory" />
              <p className="font-mono text-[10px] uppercase tracking-[0.25em] text-on-surface-variant">
                The record remains open. Act 48 has not yet been written.
              </p>
            </div>
          </main>
        </div>

        <footer className="mt-20 border-t border-ink/20 pt-4 flex flex-wrap gap-x-10 gap-y-2 font-mono text-[9.5px] uppercase tracking-widest text-on-surface-variant">
          <span>Sutra ledger, append-only</span>
          <span>Projection EmployeeDetailProjection v412</span>
          <span>This document is the system of record, rendered</span>
        </footer>
      </div>
    </div>
  )
}
