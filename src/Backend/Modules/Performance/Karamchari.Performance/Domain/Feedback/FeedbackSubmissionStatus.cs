// -----------------------------------------------------------------------
// <copyright file="FeedbackSubmissionStatus.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Feedback;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum FeedbackSubmissionStatus
{
    Draft = 1,
    Submitted = 2,
    SharedWithReviewee = 3,
    Acknowledged = 4
}
