using System.Text.RegularExpressions;
using Karamchari.Notifications.Domain;
using Microsoft.Extensions.Logging;

namespace Karamchari.Notifications.Rendering;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed partial class TemplateRenderer : ITemplateRenderer
{
    private static readonly Regex PlaceholderRegex = BuildPlaceholderRegex();

    private readonly ILogger<TemplateRenderer> _logger;

    public TemplateRenderer(ILogger<TemplateRenderer> logger) => _logger = logger;

    public (string Subject, string BodyHtml, string BodyText) Render(
        NotificationTemplate notificationTemplate,
        IReadOnlyDictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(notificationTemplate);
        ArgumentNullException.ThrowIfNull(variables);

        return (
            Replace(notificationTemplate.Subject, variables, "subject"),
            Replace(notificationTemplate.BodyHtml, variables, "bodyHtml"),
            Replace(notificationTemplate.BodyText, variables, "bodyText"));
    }

    private string Replace(string input, IReadOnlyDictionary<string, string> variables, string fieldName)
    {
        return PlaceholderRegex.Replace(input, match =>
        {
            var key = match.Groups["key"].Value;
            if (variables.TryGetValue(key, out var value))
                return value;

            _logger.LogWarning(
                "Template variable '{Key}' missing in {Field}; substituting empty string", key, fieldName);
            return string.Empty;
        });
    }

    [GeneratedRegex(@"\{\{(?<key>[A-Za-z0-9_.]+)\}\}", RegexOptions.Compiled)]
    private static partial Regex BuildPlaceholderRegex();
}
