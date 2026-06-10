// -----------------------------------------------------------------------
// <copyright file="ImportPreviewResult.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.API;

public record ImportPreviewResult(
    Guid JobId,
    string ImportType,
    int TotalRowsPreviewed,
    IEnumerable<object> Rows);
