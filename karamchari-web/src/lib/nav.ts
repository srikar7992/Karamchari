export type PageId =
  | 'pulse'
  | 'executive'
  | 'observatory'
  | 'manager'
  | 'my-dashboard'
  | 'attendance'
  | 'team-attendance'
  | 'shift-definitions'
  | 'approvals'
  | 'notifications'
  | 'roster'
  | 'payroll'
  | 'payroll-run'
  | 'period-finalization'
  | 'payslip-browser'
  | 'directory'
  | 'employee-360'
  | 'onboarding'
  | 'candidates'
  | 'candidate-detail'
  | 'offer-management'
  | 'succession'
  | 'compliance'
  | 'workflow-studio'
  | 'leave-policies'
  | 'approval-rules'
  | 'asset-registry'
  | 'asset-detail'
  | 'asset-assignment'
  | 'my-profile'
  | 'my-benefits'
  | 'my-learning'
  | 'my-performance'
  | 'my-payroll'
  | 'my-time'
  | 'team-overview'
  | 'team-performance'
  | 'team-learning'
  | 'team-compensation'

export type Persona = 'EMP' | 'MGR' | 'EXEC' | 'CMP' | 'SYS'

export interface NavItem {
  id: PageId
  label: string
  icon: string
  persona: Persona
}

export interface NavGroup {
  label: string
  rhythm: string
  sanskrit: string
  layer: string
  items: NavItem[]
}

// Ordered by cadence of human contact (design/DOCTRINE.md §2), not layer number.
// `rhythm` names the band as a movement of institutional life, not a module folder.
export const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Manager Workspace',
    rhythm: 'Manager Workspace',
    sanskrit: 'प्रबंधक-क्षेत्र',
    layer: 'Karma · Nāyakatva',
    items: [
      { id: 'team-overview',     label: 'Team Overview',     icon: 'groups',          persona: 'MGR' },
      { id: 'team-performance',  label: 'Team Performance',  icon: 'trending_up',     persona: 'MGR' },
      { id: 'team-learning',     label: 'Team Learning',     icon: 'school',          persona: 'MGR' },
      { id: 'team-compensation', label: 'Team Compensation', icon: 'payments',        persona: 'MGR' },
    ],
  },
  {
    label: 'Self Service',
    rhythm: 'Employee Self Service',
    sanskrit: 'स्व-सेवा',
    layer: 'Karma · Svātantrya',
    items: [
      { id: 'my-profile',     label: 'My Profile',     icon: 'manage_accounts', persona: 'EMP' },
      { id: 'my-time',        label: 'My Time',        icon: 'schedule',        persona: 'EMP' },
      { id: 'my-payroll',     label: 'My Payroll',     icon: 'receipt_long',    persona: 'EMP' },
      { id: 'my-benefits',    label: 'My Benefits',    icon: 'health_and_safety', persona: 'EMP' },
      { id: 'my-learning',    label: 'My Learning',    icon: 'school',          persona: 'EMP' },
      { id: 'my-performance', label: 'My Performance', icon: 'trending_up',     persona: 'EMP' },
    ],
  },
  {
    label: 'Daily',
    rhythm: 'Daily Rhythm',
    sanskrit: 'दैनिक',
    layer: 'Ops · Pulse',
    items: [
      { id: 'pulse', label: 'System Pulse', icon: 'monitoring', persona: 'SYS' },
      { id: 'my-dashboard', label: 'My Dashboard', icon: 'person', persona: 'EMP' },
      { id: 'attendance', label: 'Attendance', icon: 'where_to_vote', persona: 'EMP' },
      { id: 'team-attendance', label: 'Team Board', icon: 'pin_drop', persona: 'MGR' },
      { id: 'approvals', label: 'Approvals', icon: 'fact_check', persona: 'MGR' },
      { id: 'notifications', label: 'Notifications', icon: 'notifications', persona: 'EMP' },
    ],
  },
  {
    label: 'Weekly',
    rhythm: 'Weekly Operations',
    sanskrit: 'साप्ताहिक',
    layer: 'Ops · Topology',
    items: [
      { id: 'manager', label: 'Team Pulse', icon: 'groups', persona: 'MGR' },
      { id: 'roster', label: 'Roster Builder', icon: 'calendar_month', persona: 'MGR' },
      { id: 'directory', label: 'Directory', icon: 'group_search', persona: 'MGR' },
      { id: 'employee-360', label: 'Employee 360', icon: 'badge', persona: 'MGR' },
      { id: 'onboarding', label: 'Onboarding', icon: 'flag', persona: 'MGR' },
      { id: 'candidates', label: 'Candidates', icon: 'linear_scale', persona: 'MGR' },
      { id: 'candidate-detail', label: 'Candidate Dossier', icon: 'contact_page', persona: 'MGR' },
      { id: 'offer-management', label: 'Offers', icon: 'history_edu', persona: 'MGR' },
      { id: 'asset-registry', label: 'Asset Registry', icon: 'inventory_2', persona: 'MGR' },
      { id: 'asset-detail', label: 'Asset Detail', icon: 'inventory', persona: 'MGR' },
      { id: 'asset-assignment', label: 'Asset Custody', icon: 'assignment_ind', persona: 'MGR' },
    ],
  },
  {
    label: 'Monthly',
    rhythm: 'Monthly Governance',
    sanskrit: 'मासिक',
    layer: 'Buddhi · Kāla',
    items: [
      { id: 'executive', label: 'Executive', icon: 'insights', persona: 'EXEC' },
      { id: 'observatory', label: 'Observatory', icon: 'hub', persona: 'EXEC' },
      { id: 'succession', label: 'Succession', icon: 'account_tree', persona: 'EXEC' },
      { id: 'payroll', label: 'Payroll Cockpit', icon: 'payments', persona: 'MGR' },
      { id: 'payroll-run', label: 'Run Console', icon: 'play_circle', persona: 'MGR' },
      { id: 'period-finalization', label: 'Period Close', icon: 'event_available', persona: 'MGR' },
      { id: 'payslip-browser', label: 'Payslips', icon: 'receipt_long', persona: 'EMP' },
    ],
  },
  {
    label: 'Rarely',
    rhythm: 'Institutional Memory',
    sanskrit: 'कदाचित्',
    layer: 'Sutra · Sthiti',
    items: [
      { id: 'compliance', label: 'Compliance', icon: 'gavel', persona: 'CMP' },
      { id: 'shift-definitions', label: 'Shift Definitions', icon: 'schedule', persona: 'SYS' },
      { id: 'workflow-studio', label: 'Workflow Studio', icon: 'account_tree', persona: 'SYS' },
      { id: 'leave-policies', label: 'Leave Policies', icon: 'event_busy', persona: 'SYS' },
      { id: 'approval-rules', label: 'Approval Rules', icon: 'rule', persona: 'SYS' },
    ],
  },
]

// Telemetry band metadata per surface (DOCTRINE.md §5) — rendered by AppShell.
export const PAGE_TELEMETRY: Record<PageId, { surface: string; layer: string }> = {
  pulse: { surface: 'SRF-PULSE-001', layer: 'L01 SPANDA' },
  executive: { surface: 'SRF-EXEC-014', layer: 'L04 BUDDHI' },
  observatory: { surface: 'SRF-EXEC-015', layer: 'L04 BUDDHI' },
  manager: { surface: 'SRF-MGR-022', layer: 'L02 KARMA' },
  'my-dashboard': { surface: 'SRF-EMP-008', layer: 'L02 KARMA' },
  attendance: { surface: 'SRF-OPS-031', layer: 'L02 KARMA' },
  'team-attendance': { surface: 'SRF-OPS-033', layer: 'L02 KARMA' },
  'shift-definitions': { surface: 'SRF-ADM-091', layer: 'L05 STHITI' },
  approvals: { surface: 'SRF-FLW-038', layer: 'L03 PRAVAHA' },
  notifications: { surface: 'SRF-PLS-005', layer: 'L01 SPANDA' },
  directory: { surface: 'SRF-TOP-061', layer: 'L06 JALA' },
  roster: { surface: 'SRF-OPS-044', layer: 'L03 PRAVAHA' },
  payroll: { surface: 'SRF-PAY-051', layer: 'L02 KARMA' },
  'payroll-run': { surface: 'SRF-PAY-052', layer: 'L02 KARMA' },
  'period-finalization': { surface: 'SRF-OPS-047', layer: 'L02 KARMA' },
  'payslip-browser': { surface: 'SRF-PAY-053', layer: 'L02 KARMA' },
  'employee-360': { surface: 'SRF-TOP-063', layer: 'L06 JALA' },
  onboarding: { surface: 'SRF-TOP-068', layer: 'L06 JALA' },
  candidates: { surface: 'SRF-TOP-071', layer: 'L06 JALA' },
  'candidate-detail': { surface: 'SRF-TOP-072', layer: 'L06 JALA' },
  'offer-management': { surface: 'SRF-FLW-042', layer: 'L03 PRAVAHA' },
  succession: { surface: 'SRF-KAL-077', layer: 'L07 KALA' },
  compliance: { surface: 'SRF-DHR-082', layer: 'L08 DHARMA' },
  'workflow-studio': { surface: 'SRF-FLW-040', layer: 'L03 PRAVAHA' },
  'leave-policies': { surface: 'SRF-ADM-092', layer: 'L05 STHITI' },
  'approval-rules': { surface: 'SRF-FLW-041', layer: 'L03 PRAVAHA' },
  'asset-registry': { surface: 'SRF-AST-101', layer: 'L05 STHITI' },
  'asset-detail': { surface: 'SRF-AST-102', layer: 'L05 STHITI' },
  'asset-assignment': { surface: 'SRF-AST-103', layer: 'L05 STHITI' },
  'my-profile':        { surface: 'SRF-ESS-101', layer: 'L06 JALA' },
  'my-benefits':       { surface: 'SRF-ESS-102', layer: 'L02 KARMA' },
  'my-learning':       { surface: 'SRF-ESS-103', layer: 'L07 KALA' },
  'my-performance':    { surface: 'SRF-ESS-104', layer: 'L04 BUDDHI' },
  'my-payroll':        { surface: 'SRF-ESS-105', layer: 'L02 KARMA' },
  'my-time':           { surface: 'SRF-ESS-106', layer: 'L02 KARMA' },
  'team-overview':     { surface: 'SRF-MGR-023', layer: 'L06 JALA' },
  'team-performance':  { surface: 'SRF-MGR-024', layer: 'L04 BUDDHI' },
  'team-learning':     { surface: 'SRF-MGR-025', layer: 'L07 KALA' },
  'team-compensation': { surface: 'SRF-MGR-026', layer: 'L02 KARMA' },
}

// Surface context exposed in the chrome (cavecrew §5): which archetype the page
// is, and which laws are live on it. Archetype mirrors each page's
// PAGE_CLASSIFICATION; laws are listed only where a real registered law governs
// the surface (design/LAWBOOK.md) — absent where none applies.
export const PAGE_CONTEXT: Record<PageId, { archetype: string; laws?: string[] }> = {
  pulse: { archetype: 'command-center' },
  executive: { archetype: 'dashboard' },
  observatory: { archetype: 'dashboard' },
  manager: { archetype: 'dashboard' },
  'my-dashboard': { archetype: 'dashboard' },
  attendance: { archetype: 'topology', laws: ['CONT-LIMIT-01', 'OPS-SAFE-03'] },
  'team-attendance': { archetype: 'command-center' },
  'shift-definitions': { archetype: 'administration' },
  approvals: { archetype: 'approval' },
  notifications: { archetype: 'timeline' },
  directory: { archetype: 'registry' },
  roster: { archetype: 'builder' },
  payroll: { archetype: 'command-center' },
  'payroll-run': { archetype: 'command-center' },
  'period-finalization': { archetype: 'command-center' },
  'payslip-browser': { archetype: 'record-detail' },
  'employee-360': { archetype: 'record-detail' },
  onboarding: { archetype: 'case' },
  candidates: { archetype: 'registry' },
  'candidate-detail': { archetype: 'record-detail' },
  'offer-management': { archetype: 'approval' },
  succession: { archetype: 'topology' },
  compliance: { archetype: 'dashboard' },
  'workflow-studio': { archetype: 'builder', laws: ['GF-02', 'WF-RULE-007'] },
  'leave-policies': { archetype: 'administration' },
  'approval-rules': { archetype: 'administration' },
  'asset-registry': { archetype: 'registry' },
  'asset-detail': { archetype: 'record-detail' },
  'asset-assignment': { archetype: 'registry' },
  'my-profile':        { archetype: 'record-detail' },
  'my-benefits':       { archetype: 'record-detail' },
  'my-learning':       { archetype: 'registry' },
  'my-performance':    { archetype: 'dashboard' },
  'my-payroll':        { archetype: 'registry' },
  'my-time':           { archetype: 'dashboard', laws: ['LV-SLA-02'] },
  'team-overview':     { archetype: 'registry' },
  'team-performance':  { archetype: 'approval' },
  'team-learning':     { archetype: 'registry' },
  'team-compensation': { archetype: 'registry' },
}

// Institutional situational awareness, surfaced at the head of the directory.
export const INSTITUTIONAL_STATE = {
  cycle: 'Jun 2026',
  activeWorkflows: 24,
  openRisks: 3,
  inFlight: 214,
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
