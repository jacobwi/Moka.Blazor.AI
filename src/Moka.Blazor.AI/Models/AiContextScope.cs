namespace Moka.Blazor.AI.Models;

/// <summary>
///     Represents a named scope entry for focused context building.
/// </summary>
/// <param name="Key">Scope identifier (e.g. "path", "selection", "object").</param>
/// <param name="Data">Scope data whose type depends on the context builder implementation.</param>
/// <param name="Label">Optional human-readable label for display purposes.</param>
public sealed record AiContextScope(string Key, object? Data, string? Label = null);
