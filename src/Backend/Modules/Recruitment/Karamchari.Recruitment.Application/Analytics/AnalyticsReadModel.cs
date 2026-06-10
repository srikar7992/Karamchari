// -----------------------------------------------------------------------
// <copyright file="AnalyticsReadModel.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;

namespace Karamchari.Recruitment.Application.Analytics;

public sealed class AnalyticsReadModel : ITenantOwned
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public Guid? RequisitionId { get; set; }
    public Guid? CandidateId { get; set; }
    public Guid? ApplicationId { get; set; }
    public DateTimeOffset EventTimestamp { get; set; }
    public string AdditionalData { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
}
