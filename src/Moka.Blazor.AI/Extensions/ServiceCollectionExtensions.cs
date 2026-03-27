using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moka.Blazor.AI.Models;
using Moka.Blazor.AI.Services;
using OllamaSharp;
using OpenAI;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Moka.Blazor.AI.Extensions;

/// <summary>
///     Extension methods for registering Moka.Blazor.AI services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers the AI chat services with default settings (LM Studio at localhost:1234).
	/// </summary>
	public static IServiceCollection AddMokaAi(this IServiceCollection services) => services.AddMokaAi(_ => { });

	/// <summary>
	///     Registers the AI chat services with custom configuration.
	/// </summary>
	public static IServiceCollection AddMokaAi(
		this IServiceCollection services,
		Action<AiChatOptions> configure)
	{
		services.Configure(configure);

		var options = new AiChatOptions();
		configure(options);

		TryAddChatClient(services, options);

		services.TryAddScoped<AiChatService>();

		return services;
	}

	/// <summary>
	///     Registers the AI chat services with a custom <see cref="IChatClient" />.
	/// </summary>
	public static IServiceCollection AddMokaAi(
		this IServiceCollection services,
		IChatClient chatClient,
		Action<AiChatOptions>? configure = null)
	{
		services.Configure(configure ?? (_ => { }));
		services.TryAddSingleton(chatClient);
		services.TryAddScoped<AiChatService>();

		return services;
	}

	private static void TryAddChatClient(IServiceCollection services, AiChatOptions options)
	{
		// Don't register if consumer already provided one
		if (services.Any(d => d.ServiceType == typeof(IChatClient)))
		{
			return;
		}

		switch (options.Provider)
		{
			case AiProvider.Ollama:
				services.TryAddSingleton<IChatClient>(_ => new OllamaApiClient(
					new Uri(options.ResolvedEndpoint),
					options.DefaultModel ?? "llama3.2"));
				break;

			default: // OpenAI-compatible (LM Studio, vLLM, etc.)
				services.TryAddSingleton<IChatClient>(_ => new OpenAIClient(
						new ApiKeyCredential("lm-studio"),
						new OpenAIClientOptions { Endpoint = new Uri(options.ResolvedEndpoint) })
					.GetChatClient(options.DefaultModel ?? "local-model")
					.AsIChatClient());
				break;
		}
	}
}
