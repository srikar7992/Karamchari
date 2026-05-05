namespace Karamchari.PSA.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Defines the billing model for a client engagement.
/// </summary>
public enum BillingType
{
    /// <summary>Bill the client per hour worked (T&amp;M engagements).</summary>
    TimeAndMaterial = 0,

    /// <summary>Fixed project price regardless of hours (milestones based).</summary>
    FixedBid = 1,

    /// <summary>Internal project; no client billing generated.</summary>
    NonBillable = 2
}

/// <summary>
/// Represents a billable engagement or project under a <see cref="Client"/>.
/// </summary>
public sealed class ClientProject : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the parent client identifier.</summary>
    public Guid ClientId { get; private set; }

    /// <summary>Gets the project name (e.g., "Alpha Portal Migration").</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the billing model for this project.</summary>
    public BillingType BillingType { get; private set; }

    /// <summary>Gets the project start date.</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Gets the project end date (null means open-ended).</summary>
    public DateOnly? EndDate { get; private set; }

    /// <summary>Gets whether the project is currently active.</summary>
    public bool IsActive { get; private set; }

    private ClientProject() { }

    /// <summary>
    /// Creates a new <see cref="ClientProject"/>.
    /// </summary>
    public static ClientProject Create(
        Guid clientId,
        string name,
        BillingType billingType,
        DateOnly startDate,
        DateOnly? endDate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (clientId == Guid.Empty) throw new ArgumentException("ClientId must not be empty.", nameof(clientId));

        return new ClientProject
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            Name = name.Trim(),
            BillingType = billingType,
            StartDate = startDate,
            EndDate = endDate,
            IsActive = true
        };
    }

    /// <summary>Closes the project on the given date.</summary>
    public void Close(DateOnly closedOn)
    {
        EndDate = closedOn;
        IsActive = false;
    }
}
