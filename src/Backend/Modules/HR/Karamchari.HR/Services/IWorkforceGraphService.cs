// -----------------------------------------------------------------------
// <copyright file="IWorkforceGraphService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.HR.Domain.Organization;
using Karamchari.HR.Domain.Organization.Projections;

namespace Karamchari.HR.Services;

/// <summary>
/// Defines the contract for querying the organizational workforce graph structure,
/// including direct connections and hierarchical reporting traversals.
/// </summary>
public interface IWorkforceGraphService
{
    /// <summary>
    /// Retrieves active neighbor nodes connected to the specified source node via a specific edge relationship type.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="sourceNodeId">The ID of the source node to query from.</param>
    /// <param name="edgeType">The relationship edge type to traverse.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of related <see cref="WorkforceGraphNode"/> entities.</returns>
    Task<IEnumerable<WorkforceGraphNode>> GetRelatedNodesAsync(
        string tenantId,
        Guid sourceNodeId,
        WorkforceEdgeType edgeType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Recursively retrieves the position IDs of all indirect and direct reports under the specified manager position ID.
    /// </summary>
    /// <param name="tenantId">The unique identifier of the tenant context.</param>
    /// <param name="managerPositionId">The position ID of the manager at the top of the reporting subtree.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A collection of position IDs representing all descendants in the reporting hierarchy.</returns>
    Task<IEnumerable<Guid>> GetIndirectReportsAsync(
        string tenantId,
        Guid managerPositionId,
        CancellationToken cancellationToken = default);
}
