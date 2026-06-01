namespace Karamchari.Recruitment.Contracts;

/// <summary>
/// Detailed information about a candidate.
/// </summary>
public record CandidateDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber);

/// <summary>
/// Response returned after successfully creating a candidate.
/// </summary>
public record CandidateCreatedDto(Guid Id);

/// <summary>
/// Detailed information about a job requisition.
/// </summary>
public record RequisitionDto(
    Guid Id,
    string Title,
    string DepartmentId,
    Guid HiringManagerId,
    string Status,
    DateTimeOffset? TargetHireDate);

/// <summary>
/// Information about a job application.
/// </summary>
public record ApplicationDto(
    Guid Id,
    Guid CandidateId,
    Guid RequisitionId,
    string Status,
    DateTimeOffset AppliedAt);

/// <summary>
/// Information about an employment offer.
/// </summary>
public record OfferDto(
    Guid Id,
    Guid ApplicationId,
    decimal BaseSalary,
    string Currency,
    string Status,
    DateTimeOffset? IssuedAt,
    DateTimeOffset? ExpiresAt);
