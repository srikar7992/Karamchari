import { Icon } from './Icon'
import { NAV_GROUPS, PAGE_TELEMETRY, PERSONA_META, findNavItem, type PageId } from '@/lib/nav'

interface AppShellProps {
  active: PageId
  onNavigate: (id: PageId) => void
  children: React.ReactNode
}

export function AppShell({ active, onNavigate, children }: AppShellProps) {
  const activeItem = findNavItem(active)
  const persona = PERSONA_META[activeItem.persona]

  return (
    <div className="min-h-screen bg-surface text-on-surface">
      {/* Side Navigation (desktop) */}
      <nav className="hidden md:flex flex-col bg-surface h-screen w-64 border-r border-outline-variant fixed left-0 top-0 z-40">
        <div className="px-6 pt-8 pb-6 border-b border-outline-variant">
          <h1 className="font-serif font-light text-[28px] tracking-tight leading-none text-primary">
            Karamchari
          </h1>
          <p className="font-mono-label text-mono-label text-on-surface-variant uppercase mt-2">
            Institutional OS
          </p>
        </div>
        <div className="flex-1 overflow-y-auto py-4">
          {NAV_GROUPS.map((group) => (
            <div key={group.label} className="mb-5">
              <div className="px-6 mb-1 flex items-baseline justify-between">
                <span className="font-mono-label text-mono-label text-on-surface-variant uppercase">
                  {group.label}
                </span>
                <span className="font-mono-label text-[9px] tracking-[0.12em] text-outline uppercase">
                  {group.layer}
                </span>
              </div>
              <ul>
                {group.items.map((item) => {
                  const isActive = item.id === active
                  return (
                    <li key={item.id}>
                      <button
                        onClick={() => onNavigate(item.id)}
                        className={
                          isActive
                            ? 'flex items-center gap-3 px-6 py-2.5 w-full text-left text-tamra-copper font-medium border-r-2 border-tamra-copper bg-surface-container-low'
                            : 'flex items-center gap-3 px-6 py-2.5 w-full text-left text-on-surface-variant hover:text-on-surface hover:bg-surface-container-high transition-colors duration-200'
                        }
                      >
                        <Icon name={item.icon} filled={isActive} className="!text-[20px]" />
                        <span className="font-body-standard text-tabular-data">{item.label}</span>
                      </button>
                    </li>
                  )
                })}
              </ul>
            </div>
          ))}
        </div>
        {/* Persona chip — follows the active surface */}
        <div className="px-4 py-4 border-t border-outline-variant">
          <div className="flex items-center gap-3">
            <div className="w-8 h-8 bg-graphite flex items-center justify-center text-on-primary">
              <span className="font-mono-label text-mono-label">{activeItem.persona}</span>
            </div>
            <div>
              <div className="font-medium text-sm text-primary">{persona.name}</div>
              <div className="font-mono-label text-mono-label text-on-surface-variant">{persona.role}</div>
            </div>
          </div>
        </div>
      </nav>

      {/* Top Navigation */}
      <header className="flex justify-between items-center px-6 md:px-gutter w-full md:ml-64 md:w-[calc(100%-16rem)] bg-surface h-16 border-b border-outline-variant fixed top-0 right-0 z-30">
        <div className="flex items-center gap-4 md:hidden">
          <span className="font-serif font-light text-xl tracking-tight text-primary">Karamchari</span>
        </div>
        <div className="hidden md:flex flex-1 max-w-md">
          <div className="relative flex items-center group w-full">
            <Icon
              name="search"
              className="absolute left-0 !text-[20px] text-on-surface-variant group-focus-within:text-yantra-indigo pointer-events-none"
            />
            <input
              className="w-full pl-8 pr-4 py-2 bg-transparent border-b border-outline-variant focus:border-yantra-indigo focus:outline-none transition-colors font-tabular-data text-tabular-data placeholder:text-on-surface-variant"
              placeholder="Search operators, requests, registers…"
              type="text"
            />
          </div>
        </div>
        <div className="flex items-center gap-5">
          <span className="hidden lg:inline font-mono-label text-mono-label text-on-surface-variant uppercase">
            {activeItem.persona} · {persona.role}
          </span>
          <button className="text-on-surface-variant hover:text-tamra-copper transition-colors relative">
            <Icon name="notifications" className="!text-[22px]" />
            <span className="absolute top-0 right-0 w-2 h-2 bg-tamra-copper rounded-full" />
          </button>
          <button className="text-on-surface-variant hover:text-tamra-copper transition-colors">
            <Icon name="move_to_inbox" className="!text-[22px]" />
          </button>
          <button className="text-on-surface-variant hover:text-tamra-copper transition-colors">
            <Icon name="account_circle" className="!text-[22px]" />
          </button>
        </div>
      </header>

      {/* Main canvas */}
      <main className="md:ml-64 pt-16 min-h-screen yantra-grid pb-24 md:pb-8 flex flex-col">
        <div className="max-w-[1280px] mx-auto px-5 md:px-page-margin py-10 md:py-12 w-full flex-1">
          {children}
        </div>
        {/* Telemetry band — universal page skeleton, DOCTRINE §5 */}
        <div className="max-w-[1280px] mx-auto px-5 md:px-page-margin w-full">
          <div className="hairline-t mt-8 py-4 flex flex-wrap gap-x-8 gap-y-2 items-center font-mono text-[10px] tracking-[0.12em] uppercase text-on-surface-variant">
            <span>{PAGE_TELEMETRY[active].surface}</span>
            <span>{PAGE_TELEMETRY[active].layer}</span>
            <span>DATA FRESH · 00:00:42</span>
            <span>SESSION 91C3-48AF</span>
            <span className="ml-auto">SUTRA LEDGER · IMMUTABLE</span>
          </div>
        </div>
      </main>

      {/* Mobile bottom navigation */}
      <nav className="md:hidden fixed bottom-0 w-full bg-surface border-t border-outline-variant z-50 flex justify-around items-center h-16 px-2">
        {(['pulse', 'my-dashboard', 'attendance', 'manager'] as PageId[]).map((id) => {
          const item = findNavItem(id)
          const isActive = id === active
          return (
            <button
              key={id}
              onClick={() => onNavigate(id)}
              className={`flex flex-col items-center justify-center w-full h-full relative ${
                isActive ? 'text-tamra-copper' : 'text-on-surface-variant hover:text-tamra-copper'
              }`}
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
