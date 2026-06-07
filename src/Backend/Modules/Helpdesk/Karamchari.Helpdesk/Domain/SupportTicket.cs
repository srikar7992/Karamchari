using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Helpdesk.Domain;

public enum TicketStatus { Open, InProgress, PendingEmployee, Resolved, Closed }
public enum TicketPriority { Low, Medium, High, Critical }

public record SupportTicketId(Guid Value)
{
    public static SupportTicketId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

public record TicketCategoryId(Guid Value)
{
    public static TicketCategoryId New() => new(Guid.NewGuid());
    public override string ToString() => Value.ToString();
}

/// <summary>
/// Employee helpdesk support ticket. Tracks grievance/query from submission through resolution.
/// SLA clock starts on submission; breaches trigger escalation.
/// </summary>
public sealed class SupportTicket : AggregateRoot<SupportTicketId>, ITenantOwned
{
    private readonly List<TicketComment> _comments = [];

    private SupportTicket(
        SupportTicketId id,
        string tenantId,
        Guid employeeId,
        TicketCategoryId categoryId,
        string subject,
        string description,
        TicketPriority priority)
        : base(id)
    {
        TenantId = tenantId;
        EmployeeId = employeeId;
        CategoryId = categoryId;
        Subject = subject;
        Description = description;
        Priority = priority;
        Status = TicketStatus.Open;
        CreatedAt = DateTimeOffset.UtcNow;
        SlaDeadlineAt = ComputeSlaDeadline(priority, CreatedAt);
    }

    private SupportTicket() { TenantId = string.Empty; Subject = string.Empty; Description = string.Empty; }

    public string TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public TicketCategoryId CategoryId { get; private set; } = null!;
    public string Subject { get; private set; }
    public string Description { get; private set; }
    public TicketPriority Priority { get; private set; }
    public TicketStatus Status { get; private set; }
    public Guid? AssignedToHrId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset SlaDeadlineAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public bool IsSlaBreached => Status != TicketStatus.Resolved && Status != TicketStatus.Closed
                                 && DateTimeOffset.UtcNow > SlaDeadlineAt;
    public IReadOnlyList<TicketComment> Comments => _comments.AsReadOnly();

    public static SupportTicket Raise(
        string tenantId,
        Guid employeeId,
        TicketCategoryId categoryId,
        string subject,
        string description,
        TicketPriority priority = TicketPriority.Medium)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        return new SupportTicket(SupportTicketId.New(), tenantId, employeeId, categoryId, subject, description, priority);
    }

    public void Assign(Guid hrUserId)
    {
        AssignedToHrId = hrUserId;
        Status = TicketStatus.InProgress;
    }

    public void AddComment(Guid authorId, string body, bool isInternal = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(body);
        _comments.Add(new TicketComment(Guid.NewGuid(), authorId, body, isInternal, DateTimeOffset.UtcNow));
    }

    public void Resolve(string resolution)
    {
        if (Status == TicketStatus.Resolved || Status == TicketStatus.Closed)
            throw new InvalidOperationException("Ticket already resolved.");
        AddComment(AssignedToHrId ?? Guid.Empty, resolution, isInternal: false);
        Status = TicketStatus.Resolved;
        ResolvedAt = DateTimeOffset.UtcNow;
    }

    public void Close() => Status = TicketStatus.Closed;

    public void Reopen()
    {
        if (Status != TicketStatus.Resolved)
            throw new InvalidOperationException("Only resolved tickets can be reopened.");
        Status = TicketStatus.Open;
        ResolvedAt = null;
    }

    private static DateTimeOffset ComputeSlaDeadline(TicketPriority priority, DateTimeOffset from) =>
        priority switch
        {
            TicketPriority.Critical => from.AddHours(4),
            TicketPriority.High => from.AddHours(24),
            TicketPriority.Medium => from.AddHours(72),
            _ => from.AddHours(120)
        };
}

public sealed record TicketComment(
    Guid Id,
    Guid AuthorId,
    string Body,
    bool IsInternal,
    DateTimeOffset CreatedAt);

/// <summary>
/// Master list of ticket categories with routing hints.
/// </summary>
public sealed class TicketCategory : AggregateRoot<TicketCategoryId>, ITenantOwned
{
    private TicketCategory(TicketCategoryId id, string tenantId, string name, string? defaultAssigneeTeam)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        DefaultAssigneeTeam = defaultAssigneeTeam;
        IsActive = true;
    }

    private TicketCategory() { TenantId = string.Empty; Name = string.Empty; }

    public string TenantId { get; private set; }
    public string Name { get; private set; }
    public string? DefaultAssigneeTeam { get; private set; }
    public bool IsActive { get; private set; }

    public static TicketCategory Create(string tenantId, string name, string? defaultAssigneeTeam = null) =>
        new(TicketCategoryId.New(), tenantId, name, defaultAssigneeTeam);

    public void Deactivate() => IsActive = false;
}
