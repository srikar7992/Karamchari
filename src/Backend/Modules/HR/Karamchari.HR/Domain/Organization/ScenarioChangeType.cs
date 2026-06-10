// -----------------------------------------------------------------------
// <copyright file="ScenarioChangeType.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.HR.Domain.Organization;

/// <summary>
/// Defines organizational scenario action types.
/// </summary>
public enum ScenarioChangeType
{
    CreateUnit,
    MergeUnit,
    SplitUnit,
    MovePosition,
    DeactivateUnit
}
