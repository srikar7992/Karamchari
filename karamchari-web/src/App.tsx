import { useEffect, useState } from 'react'
import { AppShell } from '@/components/shell/AppShell'
import type { PageId } from '@/lib/nav'
import { PulseHome } from '@/pages/PulseHome'
import { ExecutiveDashboard } from '@/pages/ExecutiveDashboard'
import { ExecutiveObservatory } from '@/pages/ExecutiveObservatory'
import { ManagerDashboard } from '@/pages/ManagerDashboard'
import { MyDashboard } from '@/pages/MyDashboard'
import { AttendanceOps } from '@/pages/AttendanceOps'
import { TeamAttendanceBoard } from '@/pages/TeamAttendanceBoard'
import { ShiftDefinitions } from '@/pages/ShiftDefinitions'
import { ApprovalsInbox } from '@/pages/ApprovalsInbox'
import { NotificationCenter } from '@/pages/NotificationCenter'
import { EmployeeDirectory } from '@/pages/EmployeeDirectory'
import { RosterBuilder } from '@/pages/RosterBuilder'
import { PayrollCockpit } from '@/pages/PayrollCockpit'
import { PayrollRunConsole } from '@/pages/PayrollRunConsole'
import { PeriodFinalization } from '@/pages/PeriodFinalization'
import { PayslipBrowser } from '@/pages/PayslipBrowser'
import { Employee360 } from '@/pages/Employee360'
import { OnboardingCase } from '@/pages/OnboardingCase'
import { CandidatePipeline } from '@/pages/CandidatePipeline'
import { CandidateDetail } from '@/pages/CandidateDetail'
import { OfferManagement } from '@/pages/OfferManagement'
import { SuccessionWorkspace } from '@/pages/SuccessionWorkspace'
import { ComplianceDashboard } from '@/pages/ComplianceDashboard'
import { WorkflowStudio } from '@/pages/WorkflowStudio'
import { LeavePolicies } from '@/pages/LeavePolicies'
import { ApprovalRules } from '@/pages/ApprovalRules'
import { AssetRegistry } from '@/pages/AssetRegistry'
import { AssetDetail } from '@/pages/AssetDetail'
import { AssetAssignment } from '@/pages/AssetAssignment'
import { MyProfile } from '@/pages/MyProfile'
import { MyBenefits } from '@/pages/MyBenefits'
import { MyLearning } from '@/pages/MyLearning'
import { MyPerformance } from '@/pages/MyPerformance'
import { MyPayroll } from '@/pages/MyPayroll'
import { MyTime } from '@/pages/MyTime'
import { TeamOverview } from '@/pages/TeamOverview'
import { TeamPerformance } from '@/pages/TeamPerformance'
import { TeamLearning } from '@/pages/TeamLearning'
import { TeamCompensation } from '@/pages/TeamCompensation'
import { LabShell, type LabId } from '@/labs/LabShell'
import { ConstitutionalLedger } from '@/labs/ConstitutionalLedger'
import { Observatory } from '@/labs/Observatory'
import { LivingTopology } from '@/labs/LivingTopology'
import { ModernPulse } from '@/labs/ModernPulse'

// Visual experiments (design/EXPERIMENTS.md) live under #/lab/* outside the
// governed PageId space: no nav entry, no telemetry, no doctrine checks.
function labFromHash(): LabId | null {
  const m = window.location.hash.match(/^#\/?lab\/(ledger|observatory|topology|modern)$/)
  return m ? (m[1] as LabId) : null
}

function pageFromHash(): PageId {
  // Strip any query string (e.g. #/asset-detail?id=<guid>) before matching.
  const h = window.location.hash.replace(/^#\/?/, '').split('?')[0]
  const valid: PageId[] = [
    'pulse', 'executive', 'observatory', 'manager', 'my-dashboard', 'attendance', 'team-attendance',
    'shift-definitions', 'approvals', 'notifications',
    'roster', 'payroll', 'payroll-run', 'period-finalization', 'payslip-browser', 'directory', 'employee-360',
    'onboarding', 'candidates', 'candidate-detail', 'offer-management',
    'succession', 'compliance', 'workflow-studio',
    'leave-policies', 'approval-rules',
    'asset-registry', 'asset-detail', 'asset-assignment',
    'my-profile', 'my-benefits', 'my-learning', 'my-performance', 'my-payroll', 'my-time',
    'team-overview', 'team-performance', 'team-learning', 'team-compensation',
  ]
  return (valid as string[]).includes(h) ? (h as PageId) : 'pulse'
}

type Theme = 'day' | 'night'

function App() {
  const [page, setPage] = useState<PageId>(pageFromHash)
  const [lab, setLab] = useState<LabId | null>(labFromHash)
  const [theme, setTheme] = useState<Theme>(
    () => (localStorage.getItem('karam-theme') as Theme) || 'day'
  )

  useEffect(() => {
    const onHash = () => {
      setLab(labFromHash())
      setPage(pageFromHash())
      window.scrollTo({ top: 0 })
    }
    window.addEventListener('hashchange', onHash)
    return () => window.removeEventListener('hashchange', onHash)
  }, [])

  // Institutional Night Mode — one class on <html> drives shell (dark:) + .obs vars.
  useEffect(() => {
    document.documentElement.classList.toggle('dark', theme === 'night')
    localStorage.setItem('karam-theme', theme)
  }, [theme])
  const toggleTheme = () => setTheme((t) => (t === 'day' ? 'night' : 'day'))

  const navigate = (id: PageId) => {
    window.location.hash = `/${id}`
    setPage(id)
    setLab(null)
    window.scrollTo({ top: 0 })
  }

  if (lab) {
    return (
      <LabShell active={lab}>
        {lab === 'ledger' && <ConstitutionalLedger />}
        {lab === 'observatory' && <Observatory />}
        {lab === 'topology' && <LivingTopology />}
        {lab === 'modern' && <ModernPulse />}
      </LabShell>
    )
  }

  return (
    <AppShell active={page} onNavigate={navigate} theme={theme} onToggleTheme={toggleTheme}>
      {page === 'pulse' && <PulseHome onNavigate={navigate} />}
      {page === 'executive' && <ExecutiveDashboard />}
      {page === 'observatory' && <ExecutiveObservatory />}
      {page === 'manager' && <ManagerDashboard />}
      {page === 'my-dashboard' && <MyDashboard />}
      {page === 'attendance' && <AttendanceOps />}
      {page === 'team-attendance' && <TeamAttendanceBoard />}
      {page === 'shift-definitions' && <ShiftDefinitions />}
      {page === 'approvals' && <ApprovalsInbox />}
      {page === 'notifications' && <NotificationCenter />}
      {page === 'directory' && <EmployeeDirectory />}
      {page === 'roster' && <RosterBuilder />}
      {page === 'payroll' && <PayrollCockpit />}
      {page === 'payroll-run' && <PayrollRunConsole />}
      {page === 'period-finalization' && <PeriodFinalization />}
      {page === 'payslip-browser' && <PayslipBrowser />}
      {page === 'employee-360' && <Employee360 />}
      {page === 'onboarding' && <OnboardingCase />}
      {page === 'candidates' && <CandidatePipeline />}
      {page === 'candidate-detail' && <CandidateDetail />}
      {page === 'offer-management' && <OfferManagement />}
      {page === 'succession' && <SuccessionWorkspace />}
      {page === 'compliance' && <ComplianceDashboard />}
      {page === 'workflow-studio' && <WorkflowStudio />}
      {page === 'leave-policies' && <LeavePolicies />}
      {page === 'approval-rules' && <ApprovalRules />}
      {page === 'asset-registry' && <AssetRegistry />}
      {page === 'asset-detail' && <AssetDetail />}
      {page === 'asset-assignment' && <AssetAssignment />}
      {page === 'my-profile' && <MyProfile />}
      {page === 'my-benefits' && <MyBenefits />}
      {page === 'my-learning' && <MyLearning />}
      {page === 'my-performance' && <MyPerformance />}
      {page === 'my-payroll' && <MyPayroll />}
      {page === 'my-time' && <MyTime />}
      {page === 'team-overview' && <TeamOverview />}
      {page === 'team-performance' && <TeamPerformance />}
      {page === 'team-learning' && <TeamLearning />}
      {page === 'team-compensation' && <TeamCompensation />}
    </AppShell>
  )
}

export default App
