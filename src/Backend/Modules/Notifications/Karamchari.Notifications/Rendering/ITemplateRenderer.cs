// -----------------------------------------------------------------------
// <copyright file="ITemplateRenderer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Notifications.Domain;

namespace Karamchari.Notifications.Rendering;

/// <summary>
/// Renders a <see cref="NotificationTemplate"/> by substituting {{VarName}}
/// placeholders with the provided variable dictionary.
/// </summary>
public interface ITemplateRenderer
{
    /// <summary>
    /// Returns (subject, bodyHtml, bodyText) with all variables resolved.
    /// Variables missing from <paramref name="variables"/> are replaced with
    /// an empty string and a warning is logged.
    /// </summary>
    (string Subject, string BodyHtml, string BodyText) Render(
        NotificationTemplate notificationTemplate,
        IReadOnlyDictionary<string, string> variables);
}
