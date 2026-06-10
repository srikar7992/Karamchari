// -----------------------------------------------------------------------
// <copyright file="IProjectionHandler.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;

namespace Karamchari.Core.Projections;

/// <summary>
/// Projections handler interface for mapping domain events to read models in a CQRS pipeline.
/// </summary>
/// <typeparam name="TEvent">The type of the domain event.</typeparam>
public interface IProjectionHandler<in TEvent>
{
    /// <summary>
    /// Processes the event to update the relevant read model state.
    /// </summary>
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
