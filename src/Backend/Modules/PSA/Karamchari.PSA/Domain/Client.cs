// -----------------------------------------------------------------------
// <copyright file="Client.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.PSA.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents an external client company that is billed for work done.
/// </summary>
public sealed class Client : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the client company name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the GST Identification Number for Indian tax compliance.</summary>
    public string Gstin { get; private set; } = string.Empty;

    /// <summary>Gets the billing address used on invoices.</summary>
    public string BillingAddress { get; private set; } = string.Empty;

    /// <summary>Gets the billing currency code (e.g., "INR", "USD").</summary>
    public string Currency { get; private set; } = "INR";

    /// <summary>Gets whether the client is currently active.</summary>
    public bool IsActive { get; private set; }

    private Client() { }

    /// <summary>
    /// Creates a new <see cref="Client"/> aggregate.
    /// </summary>
    public static Client Create(
        string name,
        string gstin,
        string billingAddress,
        string currency = "INR")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(gstin);
        ArgumentNullException.ThrowIfNull(billingAddress);
        ArgumentNullException.ThrowIfNull(currency);

        return new Client
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Gstin = gstin.Trim(),
            BillingAddress = billingAddress.Trim(),
            Currency = currency.ToUpperInvariant(),
            IsActive = true
        };
    }

    /// <summary>Updates the client's billing details.</summary>
    public void UpdateDetails(string name, string billingAddress)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(billingAddress);
        Name = name.Trim();
        BillingAddress = billingAddress.Trim();
    }

    /// <summary>Deactivates the client (soft delete).</summary>
    public void Deactivate() => IsActive = false;
}
