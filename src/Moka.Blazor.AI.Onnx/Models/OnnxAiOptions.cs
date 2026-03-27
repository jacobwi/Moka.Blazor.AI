namespace Moka.Blazor.AI.Onnx.Models;

/// <summary>
///     Configuration options for the ONNX Runtime GenAI provider.
/// </summary>
public sealed class OnnxAiOptions
{
	/// <summary>
	///     Path to the ONNX model directory (must contain model files and genai_config.json).
	/// </summary>
	public string ModelPath { get; set; } = "";
}
