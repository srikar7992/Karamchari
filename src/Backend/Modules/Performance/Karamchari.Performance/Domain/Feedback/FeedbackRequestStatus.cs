// -----------------------------------------------------------------------
// <copyright file="FeedbackRequestStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Feedback;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum FeedbackRequestStatus
{
    Pending = 1,
    Submitted = 2,
    Declined = 3,
    Expired = 4
}
