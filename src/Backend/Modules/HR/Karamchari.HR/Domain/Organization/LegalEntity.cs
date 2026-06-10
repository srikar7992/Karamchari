// -----------------------------------------------------------------------
// <copyright file="LegalEntity.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Represents a registered legal entity (company) within the organization.
/// </summary>
public sealed class LegalEntity : AggregateRoot<Guid>, ITenantOwned
{
    private LegalEntity() { }

    private LegalEntity(Guid id, string tenantId, string name, string taxId, string registrationNumber) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        TaxId = taxId;
        RegistrationNumber = registrationNumber;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string TaxId { get; private set; } = string.Empty;
    public string RegistrationNumber { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static LegalEntity Create(string tenantId, string name, string taxId, string regNumber)
    {
        return new LegalEntity(Guid.NewGuid(), tenantId, name.Trim(), taxId.Trim(), regNumber.Trim());
    }
}
