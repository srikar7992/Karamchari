import { useEffect, useState } from 'react'
import { AppShell } from '@/components/shell/AppShell'
import type { PageId } from '@/lib/nav'
import { PulseHome } from '@/pages/PulseHome'
import { ExecutiveDashboard } from '@/pages/ExecutiveDashboard'
import { ManagerDashboard } from '@/pages/ManagerDashboard'
import { MyDashboard } from '@/pages/MyDashboard'
import { AttendanceOps } from '@/pages/AttendanceOps'
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
import { SuccessionWorkspace } from '@/pages/SuccessionWorkspace'
import { ComplianceDashboard } from '@/pages/ComplianceDashboard'

function pageFromHash(): PageId {
  const h = window.location.hash.replace(/^#\/?/, '')
  const valid: PageId[] = [
    'pulse', 'executive', 'manager', 'my-dashboard', 'attendance', 'approvals', 'notifications',
    'roster', 'payroll', 'payroll-run', 'period-finalization', 'directory', 'employee-360',
    'onboarding', 'candidates', 'succession', 'compliance',
  ]
  return (valid as string[]).includes(h) ? (h as PageId) : 'pulse'
}

function App() {
  const [page, setPage] = useState<PageId>(pageFromHash)

  useEffect(() => {
    const onHash = () => setPage(pageFromHash())
    window.addEventListener('hashchange', onHash)
    return () => window.removeEventListener('hashchange', onHash)
  }, [])

  const navigate = (id: PageId) => {
    window.location.hash = `/${id}`
    setPage(id)
    window.scrollTo({ top: 0 })
  }

  return (
    <AppShell active={page} onNavigate={navigate}>
      {page === 'pulse' && <PulseHome onNavigate={navigate} />}
      {page === 'executive' && <ExecutiveDashboard />}
      {page === 'manager' && <ManagerDashboard />}
      {page === 'my-dashboard' && <MyDashboard />}
      {page === 'attendance' && <AttendanceOps />}
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
      {page === 'succession' && <SuccessionWorkspace />}
      {page === 'compliance' && <ComplianceDashboard />}
    </AppShell>
  )
}

export default App
