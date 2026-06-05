namespace Karamchari.TimeAttendance.Domain.Rostering.Constraints;

public sealed record ConstraintResult
{
    public bool IsAllowed { get; init; }
    public string? ViolationCode { get; init; }
    public string? Reason { get; init; }

    public static ConstraintResult Allow() => new() { IsAllowed = true };

    public static ConstraintResult Deny(string code, string reason) =>
        new() { IsAllowed = false, ViolationCode = code, Reason = reason };
}
