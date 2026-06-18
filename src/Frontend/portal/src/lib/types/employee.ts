export interface EmployeeListItem {
  id: string;
  employeeNumber: string;
  legalName: string;
  workEmail: string | null;
  hiredOn: string;
  status: string;
}

export interface OnboardEmployeeCommand {
  employeeNumber: string;
  legalName: string;
  workEmail: string | null;
  hiredOn: string;
}

export interface EmployeeDetail {
  id: string;
  employeeNumber: string;
  legalName: string;
  workEmail: string | null;
  hiredOn: string;
  status: string;
  timeZoneId: string | null;
}

export type EmployeeHistoryType =
  | 'ManagerChange'
  | 'DepartmentTransfer'
  | 'DesignationChange'
  | 'BranchTransfer'
  | 'LegalEntityTransfer'
  | 'EmploymentTypeChange'
  | 'LifecycleStateChange'
  | 'TimeZoneChange';

export interface EmployeeHistoryEvent {
  type: EmployeeHistoryType;
  previous: string | null;
  new: string | null;
  effectiveFrom: string;
  effectiveTo: string | null;
  changedBy: string | null;
  loggedAt: string;
}
