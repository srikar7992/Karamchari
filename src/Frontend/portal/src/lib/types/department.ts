/**
 * Mirror of Karamchari.Api.Models.DepartmentListItem (the BFF projection).
 * If the BFF DTO grows, this type must grow with it — there is no codegen yet.
 */
export interface DepartmentListItem {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

/** Mirror of Karamchari.HR.Contracts.Organization.CreateDepartmentCommand. */
export interface CreateDepartmentCommand {
  name: string;
  description?: string | null;
}
