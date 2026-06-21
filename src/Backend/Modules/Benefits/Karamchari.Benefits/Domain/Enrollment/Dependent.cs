namespace Karamchari.Benefits.Domain.Enrollment;
public sealed record Dependent(string Name, string Relationship, DateOnly DateOfBirth, bool IsCovered);
