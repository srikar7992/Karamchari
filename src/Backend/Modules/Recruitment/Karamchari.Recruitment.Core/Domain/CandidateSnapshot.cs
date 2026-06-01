namespace Karamchari.Recruitment.Domain;

public sealed record CandidateSnapshot(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    int Version);
