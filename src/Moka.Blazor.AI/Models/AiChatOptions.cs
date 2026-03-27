namespace Moka.Blazor.AI.Models;

/// <summary>
///     Configuration options for the AI chat service.
/// </summary>
public sealed class AiChatOptions
{
	/// <summary>AI provider type. Default is <see cref="AiProvider.OpenAiCompatible" /> (works with LM Studio).</summary>
	public AiProvider Provider { get; set; } = AiProvider.OpenAiCompatible;

	/// <summary>API endpoint URL. When <c>null</c>, uses the provider default.</summary>
	public string? Endpoint { get; set; }

	/// <summary>Model name (e.g., "qwen2.5-3b", "llama3.2", "phi-3.5-mini").</summary>
	public string? DefaultModel { get; set; } = "local-model";

	/// <summary>Custom system prompt. When <c>null</c>, consumers provide their own.</summary>
	public string? SystemPrompt { get; set; }

	/// <summary>Maximum characters of context to include in the prompt. Default is 8000 (~2K tokens).</summary>
	public int MaxContextChars { get; set; } = 8000;

	/// <summary>Whether to stream responses token-by-token. Default is <c>true</c>.</summary>
	public bool StreamResponses { get; set; } = true;

	/// <summary>Generation temperature (0.0 = deterministic, 2.0 = very creative). Default is 0.3.</summary>
	public float Temperature { get; set; } = 0.3f;

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

	/// <summary>Embedded ONNX Runtime GenAI (requires Moka.Blazor.AI.Onnx package).</summary>
	Onnx
}
