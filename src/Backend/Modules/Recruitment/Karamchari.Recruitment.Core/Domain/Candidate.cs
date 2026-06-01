using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Domain;

public sealed class Candidate : AggregateRoot<CandidateId>, ITenantOwned, IAuditable
{
    private Candidate() : base(default) 
    { 
        FirstName = null!; 
        LastName = null!; 
        Email = null!; 
    }

    private Candidate(CandidateId id, string firstName, string lastName, string email)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        ProfileVersion = 1;
    }

    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public int ProfileVersion { get; private set; }

    // ITenantOwned
    public string TenantId { get; private set; } = null!;

    // IAuditable
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public string CreatedBy { get; private set; } = null!;
    public DateTimeOffset? UpdatedOnUtc { get; private set; }
    public string? UpdatedBy { get; private set; }

    public static Candidate Create(string firstName, string lastName, string email)
    {
        return new Candidate(CandidateId.New(), firstName, lastName, email);
    }

    public void UpdateProfile(string firstName, string lastName, string email, string? phoneNumber)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        PhoneNumber = phoneNumber;
        ProfileVersion++;
    }

    public CandidateSnapshot CreateSnapshot()
    {
        return new CandidateSnapshot(FirstName, LastName, Email, PhoneNumber, ProfileVersion);
    }
}
