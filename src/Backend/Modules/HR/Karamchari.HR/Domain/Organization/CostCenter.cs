// -----------------------------------------------------------------------
// <copyright file="CostCenter.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Represents a cost center for financial tracking within a tenant.
/// </summary>
public sealed class CostCenter : AggregateRoot<Guid>, ITenantOwned
{
    private CostCenter() { }

    private CostCenter(Guid id, string tenantId, string name, string code) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Code = code;
        IsActive = true;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }

    public static CostCenter Create(string tenantId, string name, string code)
    {
        return new CostCenter(Guid.NewGuid(), tenantId, name.Trim(), code.Trim().ToUpperInvariant());
    }
}
