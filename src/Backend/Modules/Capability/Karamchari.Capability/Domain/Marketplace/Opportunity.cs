using System;
using System.Collections.Generic;
using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Capability.Domain.Marketplace;

/// <summary>
/// Status states of an Opportunity.
/// </summary>
public enum OpportunityStatus
{
    /// <summary>Draft state</summary>
    Draft,
    /// <summary>Open for applications</summary>
    Open,
    /// <summary>Active work phase</summary>
    InProgress,
    /// <summary>Finished</summary>
    Completed,
    /// <summary>Revoked/Aborted</summary>
    Cancelled
}

/// <summary>
/// Types of Opportunities available.
/// </summary>
public enum OpportunityType
{
    /// <summary>Short term gig</summary>
    ShortTermGig,
    /// <summary>Mentorship posting</summary>
    Mentorship,
    /// <summary>Cross functional project assignment</summary>
    CrossFunctionalProject,
    /// <summary>Internal full time role listing</summary>
    FullTimeRole
}

/// <summary>
/// Represents an internal project, gig, or learning pathway assignment.
/// </summary>
public sealed class Opportunity : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<OpportunityRequirement> _requirements = [];

    private Opportunity()
    {
        TenantId = string.Empty;
        Title = string.Empty;
        Description = string.Empty;
        ApprovalWorkflow = string.Empty;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Opportunity"/> class.
    /// </summary>
    public Opportunity(
        Guid id,
        string tenantId,
        string title,
        string description,
        Guid managerId,
        OpportunityType type,
        int maxParticipants,
        int hoursPerWeek,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string approvalWorkflow) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        TenantId = tenantId;
        Title = title;
        Description = description ?? string.Empty;
        ManagerId = managerId;
        Type = type;
        MaxParticipants = maxParticipants;
        AllocatedHoursPerWeek = hoursPerWeek;
        StartDate = startDate;
        EndDate = endDate;
        ApprovalWorkflow = approvalWorkflow ?? string.Empty;
        Status = OpportunityStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the hiring manager or sponsor ID.
    /// </summary>
    public Guid ManagerId { get; private set; }

    /// <summary>
    /// Gets the opportunity type.
    /// </summary>
    public OpportunityType Type { get; private set; }

    /// <summary>
    /// Gets the opportunity status.
    /// </summary>
    public OpportunityStatus Status { get; private set; }

    /// <summary>
    /// Gets the maximum allowed participants.
    /// </summary>
    public int MaxParticipants { get; private set; }

    /// <summary>
    /// Gets the weekly allocated hours for participants.
    /// </summary>
    public int AllocatedHoursPerWeek { get; private set; }

    /// <summary>
    /// Gets the project start date.
    /// </summary>
    public DateTimeOffset StartDate { get; private set; }

    /// <summary>
    /// Gets the project end date.
    /// </summary>
    public DateTimeOffset EndDate { get; private set; }

    /// <summary>
    /// Gets the approval workflow description/code.
    /// </summary>
    public string ApprovalWorkflow { get; private set; }

    /// <summary>
    /// Gets the creation time.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the list of skills required for this opportunity.
    /// </summary>
    public IReadOnlyCollection<OpportunityRequirement> Requirements => _requirements.AsReadOnly();

    /// <summary>
    /// Adds a skill requirement to the opportunity.
    /// </summary>
    public void AddRequirement(Guid skillId, int minimumLevel)
    {
        _requirements.Add(new OpportunityRequirement(Guid.NewGuid(), Id, skillId, minimumLevel));
    }

    /// <summary>
    /// Enrolls a new participant, enforcing capacity constraints.
    /// </summary>
    public void EnrollParticipant(Guid employeeId, int allocatedHours, IEnumerable<OpportunityParticipant> existingParticipants)
    {
        if (Status != OpportunityStatus.Open && Status != OpportunityStatus.InProgress)
        {
            throw new InvalidOperationException("Can only enroll participants in open or in-progress opportunities.");
        }

        int activeCount = 0;
        foreach (var p in existingParticipants)
        {
            if (p.OpportunityId == Id && (p.Status == EnrollmentStatus.Approved || p.Status == EnrollmentStatus.Active))
            {
                activeCount++;
            }
        }

        if (activeCount >= MaxParticipants)
        {
            throw new InvalidOperationException($"Opportunity has reached its maximum participant limit of {MaxParticipants}.");
        }
    }
}

/// <summary>
/// Represents a specific skill prerequisite for an opportunity.
/// </summary>
public sealed class OpportunityRequirement : Entity<Guid>
{
    private OpportunityRequirement() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="OpportunityRequirement"/> class.
    /// </summary>
    public OpportunityRequirement(Guid id, Guid opportunityId, Guid skillId, int minimumLevel) : base(id)
    {
        OpportunityId = opportunityId;
        SkillId = skillId;
        MinimumLevel = minimumLevel;
    }

    /// <summary>
    /// Gets the parent opportunity identifier.
    /// </summary>
    public Guid OpportunityId { get; }

    /// <summary>
    /// Gets the required skill identifier.
    /// </summary>
    public Guid SkillId { get; }

    /// <summary>
    /// Gets the minimum skill level required.
    /// </summary>
    public int MinimumLevel { get; }
}
