// -----------------------------------------------------------------------
// <copyright file="EmployeeListItem.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Api.Models;

internal sealed record EmployeeListItem(
    Guid Id,
    string EmployeeNumber,
    string LegalName,
    string? WorkEmail,
    DateOnly HiredOn,
    string Status);
