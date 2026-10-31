using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.AI;

namespace Moka.AI.Models;

/// <summary>
///     Configuration options for the AI chat service.
/// </summary>
public sealed class AiChatOptions
{
	/// <summary>AI provider type. Default is <see cref="AiProvider.OpenAiCompatible" /> (works with LM Studio).</summary>
	public AiProvider Provider { get; set; } = AiProvider.OpenAiCompatible;

	/// <summary>API endpoint URL. When <c>null</c>, uses the provider default.</summary>
	public string? Endpoint { get; set; }

	/// <summary>
	///     API key for the provider. When <c>null</c>, uses the placeholder <c>"lm-studio"</c>
	///     (accepted by LM Studio and other key-less local servers). Set this for providers
	///     that require authentication (OpenAI, vLLM with auth, etc.).
	/// </summary>
	public string? ApiKey { get; set; }

	/// <summary>Model name (e.g., "qwen2.5-3b", "llama3.2", "phi-3.5-mini").</summary>
	public string? DefaultModel { get; set; } = "local-model";

	/// <summary>Custom system prompt. When <c>null</c>, consumers provide their own.</summary>
	public string? SystemPrompt { get; set; }

	/// <summary>Maximum characters of context to include in the prompt. Default is 8000 (~2K tokens).</summary>
	public int MaxContextChars { get; set; } = 8000;

	/// <summary>
	///     Maximum characters of conversation history to include in the prompt.
	///     When exceeded, the oldest messages are dropped first and a truncation note
	///     is added to the system prompt. Default is 12000 (~3K tokens). Set to 0 to
	///     disable the limit.
	/// </summary>
	public int MaxHistoryChars { get; set; } = 12000;

	/// <summary>Whether to stream responses token-by-token. Default is <c>true</c>.</summary>
	public bool StreamResponses { get; set; } = true;

	/// <summary>Generation temperature (0.0 = deterministic, 2.0 = very creative). Default is 0.3.</summary>
	public float Temperature { get; set; } = 0.3f;

	/// <summary>
	///     Whether to wrap the chat client with <c>FunctionInvokingChatClient</c> so the model's
	///     tool/function calls are executed automatically in a request loop. Only relevant when
	///     <see cref="Tools" /> is non-empty. Set to <c>false</c> if you provide your own
	///     function-invoking pipeline. Default is <c>true</c>.
	/// </summary>
	public bool EnableFunctionInvocation { get; set; } = true;

	/// <summary>
	///     Tools/functions available to the model. Register via <see cref="AIFunctionFactory.Create" />,
	///     any <see cref="AITool" />, or an MCP client's tool functions. Empty (default) = tool
	///     calling disabled. This list is app-level configuration and is shared between service
	///     instances (unlike per-scope runtime settings such as model or temperature).
	/// </summary>
	[SuppressMessage("Usage", "CA2227:Collection properties should be read only",
		Justification = "Settable so per-instance option snapshots can share the app-level list reference.")]
	public IList<AITool> Tools { get; set; } = [];

	/// <summary>Resolved endpoint URL with provider-specific defaults.</summary>
	public string ResolvedEndpoint => Endpoint ?? Provider switch
	{
		AiProvider.Ollama => "http://localhost:11434",
		AiProvider.Onnx => "",
		_ => "http://localhost:1234/v1"
	};
}

/// <summary>
///     AI provider backend type.
/// </summary>
public enum AiProvider
{
	/// <summary>OpenAI-compatible API (LM Studio, vLLM, etc.).</summary>
	OpenAiCompatible,

	/// <summary>Ollama native API.</summary>
	Ollama,

	/// <summary>Embedded ONNX Runtime GenAI (requires Moka.AI.Onnx package).</summary>
	Onnx
}
