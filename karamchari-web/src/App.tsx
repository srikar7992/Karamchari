import { useEffect, useState } from 'react'
import { AppShell } from '@/components/shell/AppShell'
import type { PageId } from '@/lib/nav'
import { PulseHome } from '@/pages/PulseHome'
import { ExecutiveDashboard } from '@/pages/ExecutiveDashboard'
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
import { LabShell, type LabId } from '@/labs/LabShell'
import { ConstitutionalLedger } from '@/labs/ConstitutionalLedger'
import { Observatory } from '@/labs/Observatory'
import { LivingTopology } from '@/labs/LivingTopology'

// Visual experiments (design/EXPERIMENTS.md) live under #/lab/* outside the
// governed PageId space: no nav entry, no telemetry, no doctrine checks.
function labFromHash(): LabId | null {
  const m = window.location.hash.match(/^#\/?lab\/(ledger|observatory|topology)$/)
  return m ? (m[1] as LabId) : null
}

function pageFromHash(): PageId {
  const h = window.location.hash.replace(/^#\/?/, '')
  const valid: PageId[] = [
    'pulse', 'executive', 'manager', 'my-dashboard', 'attendance', 'team-attendance',
    'shift-definitions', 'approvals', 'notifications',
    'roster', 'payroll', 'payroll-run', 'period-finalization', 'directory', 'employee-360',
    'onboarding', 'candidates', 'candidate-detail', 'offer-management',
    'succession', 'compliance', 'workflow-studio',
    'leave-policies', 'approval-rules',
  ]
  return (valid as string[]).includes(h) ? (h as PageId) : 'pulse'
}

function App() {
  const [page, setPage] = useState<PageId>(pageFromHash)
  const [lab, setLab] = useState<LabId | null>(labFromHash)

  useEffect(() => {
    const onHash = () => {
      setLab(labFromHash())
      setPage(pageFromHash())
      window.scrollTo({ top: 0 })
    }
    window.addEventListener('hashchange', onHash)
    return () => window.removeEventListener('hashchange', onHash)
  }, [])

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
      </LabShell>
    )
  }

  return (
    <AppShell active={page} onNavigate={navigate}>
      {page === 'pulse' && <PulseHome onNavigate={navigate} />}
      {page === 'executive' && <ExecutiveDashboard />}
      {page === 'manager' && <ManagerDashboard />}
      {page === 'my-dashboard' && <MyDashboard />}
      {page === 'attendance' && <AttendanceOps />}
      {page === 'team-attendance' && <TeamAttendanceBoard />}
      {page === 'shift-definitions' && <ShiftDefinitions />}
      {page === 'approvals' && <ApprovalsInbox />}
      {page === 'notifications' && <NotificationCenter />}
      {page === 'directory' && <EmployeeDirectory onNavigate={navigate} />}
      {page === 'roster' && <RosterBuilder />}
      {page === 'payroll' && <PayrollCockpit />}
      {page === 'payroll-run' && <PayrollRunConsole />}
      {page === 'period-finalization' && <PeriodFinalization />}
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
    </AppShell>
  )
}

export default App
