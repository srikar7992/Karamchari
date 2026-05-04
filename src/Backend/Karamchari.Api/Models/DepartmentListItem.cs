namespace Karamchari.Api.Models;

internal sealed record DepartmentListItem(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);
