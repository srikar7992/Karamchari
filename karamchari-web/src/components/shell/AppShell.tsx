import { Icon } from './Icon'
import { Glyph } from '@/components/observatory/surfaces'
import {
  NAV_GROUPS, PAGE_TELEMETRY, PAGE_CONTEXT, INSTITUTIONAL_STATE, PERSONA_META,
  findNavItem, type PageId,
} from '@/lib/nav'

interface AppShellProps {
  active: PageId
  onNavigate: (id: PageId) => void
  theme: 'day' | 'night'
  onToggleTheme: () => void
  children: React.ReactNode
}

// The directory is an always-dark indigo panel (Yantra Indigo — navigation /
// trust), so its type is ivory in both themes, not driven by light/dark tokens.
const IVORY = '#f4efe7'
const IVORY_70 = 'rgba(244,239,231,0.70)'
const IVORY_46 = 'rgba(244,239,231,0.46)'
const IVORY_34 = 'rgba(244,239,231,0.34)'

function StateRow({ k, v, copper = false }: { k: string; v: string; copper?: boolean }) {
  return (
    <div className="flex items-baseline justify-between gap-3">
      <span style={{ color: IVORY_46 }}>{k}</span>
      <span className={copper ? 'text-tamra-copper font-medium' : ''} style={copper ? undefined : { color: IVORY }}>{v}</span>
    </div>
  )
}

// Background chakra — faint construction yantra (squares · circles · triangles · spokes).
function ChakraWatermark() {
  return (
    <div className="kx-watermark" aria-hidden="true">
      <svg viewBox="0 0 200 200" fill="none" stroke="currentColor" strokeWidth="0.5">
        <rect x="20" y="20" width="160" height="160" />
        <rect x="40" y="40" width="120" height="120" />
        <circle cx="100" cy="100" r="80" /><circle cx="100" cy="100" r="58" /><circle cx="100" cy="100" r="34" />
        <path d="M100 28 L160 150 L40 150 Z" /><path d="M100 172 L40 50 L160 50 Z" />
        <path d="M100 44 L148 134 L52 134 Z" /><path d="M100 156 L52 66 L148 66 Z" />
        <line x1="100" y1="20" x2="100" y2="180" /><line x1="20" y1="100" x2="180" y2="100" />
        <line x1="34" y1="34" x2="166" y2="166" /><line x1="166" y1="34" x2="34" y2="166" />
      </svg>
    </div>
  )
}

export function AppShell({ active, onNavigate, theme, onToggleTheme, children }: AppShellProps) {
  const activeItem = findNavItem(active)
  const persona = PERSONA_META[activeItem.persona]
  const ctx = PAGE_CONTEXT[active]
  const tel = PAGE_TELEMETRY[active]

  return (
    <div className="min-h-screen bg-surface text-on-surface dark:bg-[#171a1f] dark:text-[#ece6db] relative">
      <ChakraWatermark />

      {/* Institutional Directory (desktop) — Yantra Indigo panel, a library index */}
      <nav
        className="hidden md:flex flex-col h-screen w-72 fixed left-0 top-0 z-40 bg-[#1a2b47] dark:bg-[#0a0b0e]"
        style={{ borderRight: '1px solid rgba(0,0,0,0.28)' }}
      >
        {/* Masthead — sutra glyph + wordmark */}
        <div className="px-6 pt-7 pb-4 flex items-center gap-3" style={{ borderBottom: `1px solid rgba(244,239,231,0.11)` }}>
          <Glyph size={26} stroke={IVORY} bindu="#c68a60" />
          <div>
            <div className="flex items-baseline gap-2">
              <h1 className="font-serif font-light text-[24px] tracking-tight leading-none" style={{ color: IVORY }}>
                Karamchari
              </h1>
              <span className="font-serif text-[15px] leading-none" style={{ color: IVORY_70 }}>कर्मचारी</span>
            </div>
            <p className="font-mono-label text-[9px] tracking-[0.16em] uppercase mt-1.5" style={{ color: IVORY_46 }}>
              Institutional OS
            </p>
          </div>
        </div>

        {/* Institutional State */}
        <div className="px-6 pb-5" style={{ borderBottom: `1px solid rgba(244,239,231,0.11)` }}>
          <div className="font-mono-label text-[9px] tracking-[0.16em] uppercase mb-2.5" style={{ color: IVORY_34 }}>
            Institutional State
          </div>
          <div className="space-y-1.5 font-mono text-[10px] tracking-[0.06em] uppercase">
            <StateRow k="Cycle" v={INSTITUTIONAL_STATE.cycle} />
            <StateRow k="Active Workflows" v={String(INSTITUTIONAL_STATE.activeWorkflows)} />
            <StateRow k="Open Risks" v={String(INSTITUTIONAL_STATE.openRisks)} copper />
            <StateRow k="In-Flight" v={String(INSTITUTIONAL_STATE.inFlight)} />
          </div>
        </div>

        {/* Directory — macro spine threading the rhythms of institutional life */}
        <div className="flex-1 overflow-y-auto py-5 relative">
          <div className="absolute left-[26px] top-7 bottom-5 w-px" style={{ background: 'rgba(244,239,231,0.16)' }} />
          {NAV_GROUPS.map((group) => {
            const bandActive = group.items.some((i) => i.id === active)
            return (
              <div key={group.label} className="mb-6 relative">
                <span
                  className="absolute left-[22px] top-[5px] w-2 h-2 rotate-45"
                  style={{
                    background: bandActive ? 'var(--tw-copper, #b16a3c)' : 'rgba(244,239,231,0.28)',
                    backgroundColor: bandActive ? '#b16a3c' : 'rgba(244,239,231,0.28)',
                    boxShadow: bandActive ? '0 0 0 3px rgba(177,106,60,0.22)' : undefined,
                  }}
                />
                <div className="pl-11 pr-6 mb-2">
                  <div className="flex items-baseline gap-2">
                    <div
                      className="font-mono-label text-[10px] tracking-[0.16em] uppercase"
                      style={{ color: bandActive ? '#c68a60' : 'rgba(244,239,231,0.82)' }}
                    >
                      {group.rhythm}
                    </div>
                    <span className="font-serif text-[12px] leading-none" style={{ color: bandActive ? '#c68a60' : IVORY_34 }}>
                      {group.sanskrit}
                    </span>
                  </div>
                  <div className="font-mono-label text-[8.5px] tracking-[0.14em] uppercase mt-0.5" style={{ color: IVORY_34 }}>
                    {group.layer}
                  </div>
                </div>
                <ul className="pl-[26px]">
                  {group.items.map((item) => {
                    const isActive = item.id === active
                    return (
                      <li key={item.id}>
                        <button
                          onClick={() => onNavigate(item.id)}
                          className="group flex items-center gap-3 w-full text-left pl-4 pr-6 py-[7px] border-l-2 transition-colors duration-150"
                          style={{
                            borderColor: isActive ? '#b16a3c' : 'transparent',
                            background: isActive ? 'rgba(244,239,231,0.07)' : 'transparent',
                          }}
                          onMouseEnter={(e) => { if (!isActive) e.currentTarget.style.background = 'rgba(244,239,231,0.05)' }}
                          onMouseLeave={(e) => { if (!isActive) e.currentTarget.style.background = 'transparent' }}
                        >
                          <span className="w-5 shrink-0 flex items-center" style={{ color: isActive ? '#c68a60' : IVORY_46 }}>
                            <Icon name={item.icon} filled={isActive} className="!text-[19px]" />
                          </span>
                          <span
                            className="font-tabular-data text-[13px]"
                            style={{ color: isActive ? '#d8a781' : IVORY_70, fontWeight: isActive ? 500 : 400 }}
                          >
                            {item.label}
                          </span>
                        </button>
                      </li>
                    )
                  })}
                </ul>
              </div>
            )
          })}
        </div>

        {/* Officer seal — copper square identity */}
        <div className="px-4 py-4 flex items-center gap-3" style={{ borderTop: `1px solid rgba(244,239,231,0.11)` }}>
          <div className="cham-sm w-8 h-8 bg-tamra-copper flex items-center justify-center shrink-0">
            <span className="font-mono-label text-[10px]" style={{ color: '#fff' }}>{activeItem.persona}</span>
          </div>
          <div className="min-w-0">
            <div className="font-medium text-[13px] truncate" style={{ color: IVORY }}>{persona.name}</div>
            <div className="font-mono-label text-[9px] tracking-[0.08em]" style={{ color: IVORY_46 }}>{persona.role}</div>
          </div>
        </div>
      </nav>

      {/* Top chrome — surface context, always visible */}
      <header className="flex justify-between items-center gap-4 px-6 md:px-gutter w-full md:ml-72 md:w-[calc(100%-18rem)] bg-surface dark:bg-[#1b1f26] h-16 border-b border-outline-variant dark:border-[#2b313a] fixed top-0 right-0 z-30">
        <div className="flex items-center gap-4 md:hidden">
          <span className="font-serif font-light text-xl tracking-tight text-primary dark:text-[#ece6db]">Karamchari</span>
        </div>
        {/* Surface context cluster: where am I in the institution */}
        <div className="hidden md:flex items-center gap-3 font-mono text-[10px] tracking-[0.12em] uppercase whitespace-nowrap overflow-hidden">
          <span className="text-on-surface dark:text-[#ece6db]">{tel.surface}</span>
          <span className="text-outline">·</span>
          <span className="text-on-surface-variant dark:text-[#9a958c]">{tel.layer}</span>
          <span className="text-outline">·</span>
          <span className="text-on-surface-variant dark:text-[#9a958c]">{ctx.archetype}</span>
          {ctx.laws && (
            <>
              <span className="text-outline">·</span>
              <span className="text-tamra-copper">LAW {ctx.laws.join(' · ')}</span>
            </>
          )}
        </div>
        <div className="flex items-center gap-5 shrink-0">
          <div className="relative hidden xl:flex items-center group w-56">
            <Icon
              name="search"
              className="absolute left-0 !text-[18px] text-on-surface-variant dark:text-[#8b8b90] group-focus-within:text-yantra-indigo pointer-events-none"
            />
            <input
              className="w-full pl-7 pr-2 py-2 bg-transparent border-b border-outline-variant dark:border-[#2b313a] focus:border-yantra-indigo focus:outline-none transition-colors font-tabular-data text-[13px] placeholder:text-on-surface-variant dark:placeholder:text-[#6f6f74] dark:text-[#ece6db]"
              placeholder="Search the institution…"
              type="text"
            />
          </div>
          {/* Day / Night — institutional lighting */}
          <button
            onClick={onToggleTheme}
            title={theme === 'day' ? 'Switch to Night Institution' : 'Switch to Day Institution'}
            className="text-on-surface-variant dark:text-[#9a958c] hover:text-tamra-copper transition-colors"
          >
            <Icon name={theme === 'day' ? 'dark_mode' : 'light_mode'} className="!text-[21px]" />
          </button>
          <button className="text-on-surface-variant dark:text-[#9a958c] hover:text-tamra-copper transition-colors relative">
            <Icon name="notifications" className="!text-[22px]" />
            <span className="absolute top-0 right-0 w-2 h-2 bg-tamra-copper rounded-full" />
          </button>
          <button className="text-on-surface-variant dark:text-[#9a958c] hover:text-tamra-copper transition-colors">
            <Icon name="move_to_inbox" className="!text-[22px]" />
          </button>
          <button className="text-on-surface-variant dark:text-[#9a958c] hover:text-tamra-copper transition-colors">
            <Icon name="account_circle" className="!text-[22px]" />
          </button>
        </div>
      </header>

      {/* Main canvas */}
      <main className="md:ml-72 pt-16 min-h-screen yantra-grid pb-24 md:pb-8 flex flex-col relative z-10">
        <div className="max-w-[1280px] mx-auto px-5 md:px-page-margin py-10 md:py-12 w-full flex-1">
          {children}
        </div>
        {/* Telemetry band — universal page skeleton, DOCTRINE §5 */}
        <div className="max-w-[1280px] mx-auto px-5 md:px-page-margin w-full">
          <div className="border-t border-outline-variant dark:border-[#2b313a] mt-8 py-4 flex flex-wrap gap-x-8 gap-y-2 items-center font-mono text-[10px] tracking-[0.12em] uppercase text-on-surface-variant dark:text-[#8b8b90]">
            <span>{tel.surface}</span>
            <span>{tel.layer}</span>
            <span>{ctx.archetype.toUpperCase()} ARCHETYPE</span>
            {ctx.laws && <span className="text-tamra-copper">ACTIVE LAW · {ctx.laws.join(' · ')}</span>}
            <span>DATA FRESH · 00:00:42</span>
            <span className="ml-auto">SUTRA LEDGER · IMMUTABLE</span>
          </div>
        </div>
      </main>

      {/* Mobile bottom navigation */}
      <nav className="md:hidden fixed bottom-0 w-full bg-[#1a2b47] dark:bg-[#0a0b0e] z-50 flex justify-around items-center h-16 px-2" style={{ borderTop: '1px solid rgba(244,239,231,0.11)' }}>
        {(['pulse', 'my-dashboard', 'attendance', 'manager'] as PageId[]).map((id) => {
          const item = findNavItem(id)
          const isActive = id === active
          return (
            <button
              key={id}
              onClick={() => onNavigate(id)}
              className="flex flex-col items-center justify-center w-full h-full relative"
              style={{ color: isActive ? '#c68a60' : IVORY_70 }}
            >
              {isActive && <span className="absolute top-0 w-8 h-[2px] bg-tamra-copper" />}
              <Icon name={item.icon} filled={isActive} className="!text-[22px] mb-1" />
              <span className="font-mono-label text-[10px] uppercase">{item.label.split(' ')[0]}</span>
            </button>
          )
        })}
      </nav>
    </div>
  )
}
