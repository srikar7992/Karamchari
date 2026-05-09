namespace Karamchari.HR.Contracts.Organization;

/// <summary>
/// Command payload for creating a tenant-scoped department.
/// </summary>
public sealed record CreateDepartmentCommand(string Name, string? Description);
