export type PageId =
  | 'pulse'
  | 'executive'
  | 'manager'
  | 'my-dashboard'
  | 'attendance'
  | 'approvals'
  | 'notifications'
  | 'roster'
  | 'payroll'
  | 'payroll-run'
  | 'period-finalization'
  | 'directory'
  | 'employee-360'
  | 'onboarding'
  | 'candidates'
  | 'succession'
  | 'compliance'

export type Persona = 'EMP' | 'MGR' | 'EXEC' | 'CMP' | 'SYS'

export interface NavItem {
  id: PageId
  label: string
  icon: string
  persona: Persona
}

export interface NavGroup {
  label: string
  layer: string
  items: NavItem[]
}

// Ordered by cadence of human contact (design/DOCTRINE.md §2), not layer number.
export const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Daily',
    layer: 'Ops · Pulse',
    items: [
      { id: 'pulse', label: 'System Pulse', icon: 'monitoring', persona: 'SYS' },
      { id: 'my-dashboard', label: 'My Dashboard', icon: 'person', persona: 'EMP' },
      { id: 'attendance', label: 'Attendance', icon: 'where_to_vote', persona: 'EMP' },
      { id: 'approvals', label: 'Approvals', icon: 'fact_check', persona: 'MGR' },
      { id: 'notifications', label: 'Notifications', icon: 'notifications', persona: 'EMP' },
    ],
  },
  {
    label: 'Weekly',
    layer: 'Ops · Topology',
    items: [
      { id: 'manager', label: 'Team Pulse', icon: 'groups', persona: 'MGR' },
      { id: 'roster', label: 'Roster Builder', icon: 'calendar_month', persona: 'MGR' },
      { id: 'directory', label: 'Directory', icon: 'group_search', persona: 'MGR' },
      { id: 'employee-360', label: 'Employee 360', icon: 'badge', persona: 'MGR' },
      { id: 'onboarding', label: 'Onboarding', icon: 'flag', persona: 'MGR' },
      { id: 'candidates', label: 'Candidates', icon: 'linear_scale', persona: 'MGR' },
    ],
  },
  {
    label: 'Monthly',
    layer: 'Buddhi · Kāla',
    items: [
      { id: 'executive', label: 'Executive', icon: 'insights', persona: 'EXEC' },
      { id: 'succession', label: 'Succession', icon: 'account_tree', persona: 'EXEC' },
      { id: 'payroll', label: 'Payroll Cockpit', icon: 'payments', persona: 'MGR' },
      { id: 'payroll-run', label: 'Run Console', icon: 'play_circle', persona: 'MGR' },
      { id: 'period-finalization', label: 'Period Close', icon: 'event_available', persona: 'MGR' },
    ],
  },
  {
    label: 'Rarely',
    layer: 'Sutra · Sthiti',
    items: [{ id: 'compliance', label: 'Compliance', icon: 'gavel', persona: 'CMP' }],
  },
]

// Telemetry band metadata per surface (DOCTRINE.md §5) — rendered by AppShell.
export const PAGE_TELEMETRY: Record<PageId, { surface: string; layer: string }> = {
  pulse: { surface: 'SRF-PULSE-001', layer: 'L01 SPANDA' },
  executive: { surface: 'SRF-EXEC-014', layer: 'L04 BUDDHI' },
  manager: { surface: 'SRF-MGR-022', layer: 'L02 KARMA' },
  'my-dashboard': { surface: 'SRF-EMP-008', layer: 'L02 KARMA' },
  attendance: { surface: 'SRF-OPS-031', layer: 'L02 KARMA' },
  approvals: { surface: 'SRF-FLW-038', layer: 'L03 PRAVAHA' },
  notifications: { surface: 'SRF-PLS-005', layer: 'L01 SPANDA' },
  directory: { surface: 'SRF-TOP-061', layer: 'L06 JALA' },
  roster: { surface: 'SRF-OPS-044', layer: 'L03 PRAVAHA' },
  payroll: { surface: 'SRF-PAY-051', layer: 'L02 KARMA' },
  'payroll-run': { surface: 'SRF-PAY-052', layer: 'L02 KARMA' },
  'period-finalization': { surface: 'SRF-OPS-047', layer: 'L02 KARMA' },
  'employee-360': { surface: 'SRF-TOP-063', layer: 'L06 JALA' },
  onboarding: { surface: 'SRF-TOP-068', layer: 'L06 JALA' },
  candidates: { surface: 'SRF-TOP-071', layer: 'L06 JALA' },
  succession: { surface: 'SRF-KAL-077', layer: 'L07 KALA' },
  compliance: { surface: 'SRF-DHR-082', layer: 'L08 DHARMA' },
}

export const PERSONA_META: Record<Persona, { name: string; role: string }> = {
  EMP: { name: 'Asha V.', role: 'Operator · Zone A' },
  MGR: { name: 'Ananya R.', role: 'Regional Director' },
  EXEC: { name: 'D. Iyer', role: 'Chief Operating Officer' },
  CMP: { name: 'S. Menon', role: 'Compliance Officer' },
  SYS: { name: 'Karamchari', role: 'Institutional OS' },
}

export function findNavItem(id: PageId): NavItem {
  for (const g of NAV_GROUPS) {
    const item = g.items.find((i) => i.id === id)
    if (item) return item
  }
  return NAV_GROUPS[0].items[0]
}
