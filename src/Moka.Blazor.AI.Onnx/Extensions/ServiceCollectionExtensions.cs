using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ML.OnnxRuntimeGenAI;
using Moka.Blazor.AI.Extensions;
using Moka.Blazor.AI.Models;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Moka.Blazor.AI.Onnx.Extensions;

/// <summary>
///     Extension methods for registering the ONNX Runtime GenAI provider with Moka.Blazor.AI.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Adds the Moka AI chat services with an embedded ONNX Runtime GenAI model.
	///     The model runs fully in-process — no external server required.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="modelPath">
	///     Path to the ONNX model directory (must contain model files and genai_config.json).
	///     Download models from Hugging Face (e.g., Phi-3.5-mini-instruct-onnx).
	/// </param>
	/// <param name="configure">Optional configuration for AI chat options.</param>
	public static IServiceCollection AddMokaAiOnnx(
		this IServiceCollection services,
		string modelPath,
		Action<AiChatOptions>? configure = null)
	{
		// Register base AI services (AiChatService, etc.)
		services.AddMokaAi(opts =>
		{
			opts.Provider = AiProvider.Onnx;
			configure?.Invoke(opts);
		});

		// Register OnnxRuntimeGenAIChatClient as IChatClient.
		// Singleton because the model loads into memory once and stays loaded.
		// Uses AddSingleton (not TryAdd) to override any default IChatClient
		// that the base AddMokaAi() may have registered.
		services.AddSingleton<IChatClient>(_ => new OnnxRuntimeGenAIChatClient(modelPath));

		return services;
	}
}
