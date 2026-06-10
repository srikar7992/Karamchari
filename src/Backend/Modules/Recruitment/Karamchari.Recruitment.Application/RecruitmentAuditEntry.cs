// -----------------------------------------------------------------------
// <copyright file="RecruitmentAuditEntry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Application;

public sealed class RecruitmentAuditEntry : ITenantOwned
{
    public Guid Id { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? OldState { get; set; }
    public string NewState { get; set; } = null!;
    public DateTimeOffset Timestamp { get; set; }
    public string UserId { get; set; } = null!;
    public Guid? CorrelationId { get; set; }
    public string TenantId { get; set; } = null!;
}
