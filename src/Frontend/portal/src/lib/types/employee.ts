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
