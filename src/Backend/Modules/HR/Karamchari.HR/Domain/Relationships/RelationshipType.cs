// -----------------------------------------------------------------------
// <copyright file="RelationshipType.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Domain.Relationships;

/// <summary>Type of reporting or functional relationship between two employees.</summary>
public enum RelationshipType
{
    /// <summary>Direct line manager: formal reporting authority.</summary>
    DirectManager = 1,

    /// <summary>Dotted-line manager: functional/project authority without formal reporting line.</summary>
    DottedLineManager = 2,

    /// <summary>Project manager: temporary authority scoped to a specific project.</summary>
    ProjectManager = 3,

    /// <summary>Mentor: formal mentorship relationship for career development.</summary>
    Mentor = 4,

    /// <summary>Skip-level manager: the manager's manager, granted visibility for calibration.</summary>
    SkipLevelManager = 5,

    /// <summary>Calibration committee member: granted read access to employee's review data.</summary>
    CalibrationCommitteeMember = 6,

    /// <summary>Acting manager: temporary authority during leave or transition.</summary>
    ActingManager = 7,
}
