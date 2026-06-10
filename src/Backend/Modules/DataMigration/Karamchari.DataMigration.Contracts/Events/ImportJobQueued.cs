// -----------------------------------------------------------------------
// <copyright file="ImportJobQueued.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.DataMigration.Contracts.Events;

/// <summary>
/// Integration event published when an import job is ready for execution.
/// </summary>
public record ImportJobQueued(
    Guid JobId,
    string TenantId,
    string ImportType);
