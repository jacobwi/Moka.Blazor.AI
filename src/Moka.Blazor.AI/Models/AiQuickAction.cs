namespace Moka.Blazor.AI.Models;

/// <summary>
///     A configurable quick action button for the AI chat panel.
/// </summary>
/// <param name="Label">Button display text (e.g., "Summarize").</param>
/// <param name="Prompt">The prompt to send when clicked.</param>
/// <param name="Tooltip">Optional tooltip text.</param>
public sealed record AiQuickAction(string Label, string Prompt, string? Tooltip = null);
