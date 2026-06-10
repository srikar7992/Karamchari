// -----------------------------------------------------------------------
// <copyright file="DepartmentListItem.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.Models;

internal sealed record DepartmentListItem(
    Guid Id,
    string Name,
    string? Description,
    bool IsActive);
