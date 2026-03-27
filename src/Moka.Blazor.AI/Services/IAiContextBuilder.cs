using Moka.Blazor.AI.Models;

namespace Moka.Blazor.AI.Services;

/// <summary>
///     Provides domain-specific context for the AI assistant.
///     Implementors extract relevant content from their data source (JSON document, code file, etc.)
///     to include in the AI prompt.
/// </summary>
public interface IAiContextBuilder
{
	/// <summary>
	///     Builds a context string from the source data, respecting the max context size.
	///     Implementations should check <see cref="GetScopes" /> to narrow the context
	///     when scopes have been set.
	/// </summary>
	/// <param name="options">AI options including <see cref="AiChatOptions.MaxContextChars" />.</param>
	/// <returns>Context string to include in the system prompt.</returns>
	string BuildContext(AiChatOptions options);

	/// <summary>
	///     Sets a named scope to focus context building on specific data.
	///     For example, a JSON context builder might accept a <c>"path"</c> scope
	///     to return only a subtree instead of the full document.
	/// </summary>
	/// <param name="key">Scope identifier (e.g. "path", "selection", "object").</param>
	/// <param name="data">Scope data whose type depends on the implementation.</param>
	void SetScope(string key, object? data)
	{
	}

	/// <summary>
	///     Clears a named scope, or all scopes if <paramref name="key" /> is <c>null</c>.
	/// </summary>
	/// <param name="key">Scope to clear, or <c>null</c> to clear all.</param>
	void ClearScope(string? key = null)
	{
	}

	/// <summary>
	///     Returns all currently active scopes.
	/// </summary>
	IReadOnlyDictionary<string, object?> GetScopes() => new Dictionary<string, object?>();
}
