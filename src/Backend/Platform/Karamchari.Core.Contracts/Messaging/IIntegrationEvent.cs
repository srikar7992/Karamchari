// -----------------------------------------------------------------------
// <copyright file="IIntegrationEvent.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Contracts;

/// <summary>
/// Marker interface for integration events used to communicate between bounded
/// contexts. Lives in the zero-dependency Contracts assembly so every module's
/// <c>*.Contracts</c> project can implement it without a heavy reference.
/// Enforced by the event-versioning architecture test (finding H-1); see
/// docs/development/standards/event-versioning-standard.md.
/// </summary>
public interface IIntegrationEvent
{
}
