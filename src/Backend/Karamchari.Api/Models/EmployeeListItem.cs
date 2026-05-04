namespace Karamchari.Api.Models;

internal sealed record EmployeeListItem(
    Guid Id,
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    DateOnly HiredOn,
    string Status);
