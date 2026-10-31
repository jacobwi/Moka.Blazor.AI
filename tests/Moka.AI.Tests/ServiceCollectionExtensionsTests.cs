using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Moka.AI.Extensions;
using Moka.AI.Models;
using Moka.AI.Services;
using OllamaSharp;
using Xunit;

namespace Moka.AI.Tests;

public sealed class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddMokaAi_RegistersChatServiceAndClient()
	{
		var services = new ServiceCollection();

		services.AddMokaAi(_ => { });

		using var provider = services.BuildServiceProvider();
		Assert.NotNull(provider.GetRequiredService<AiChatService>());
		Assert.NotNull(provider.GetRequiredService<IChatClient>());
	}

	[Fact]
	public void AddMokaAi_DoesNotOverride_CustomChatClient()
	{
		var services = new ServiceCollection();
		using var custom = new FakeChatClient();
		services.AddSingleton<IChatClient>(custom);

		services.AddMokaAi(_ => { });

		using var provider = services.BuildServiceProvider();
		Assert.Same(custom, provider.GetRequiredService<IChatClient>());
	}

	[Fact]
	public void AddMokaAi_WithClientInstance_UsesProvidedClient()
	{
		var services = new ServiceCollection();
		using var custom = new FakeChatClient();

		services.AddMokaAi(custom);

		using var provider = services.BuildServiceProvider();
		Assert.Same(custom, provider.GetRequiredService<IChatClient>());
	}

	[Fact]
	public void AddMokaAi_Ollama_RegistersOllamaApiClient()
	{
		var services = new ServiceCollection();

		services.AddMokaAi(o => o.Provider = AiProvider.Ollama);

		using var provider = services.BuildServiceProvider();
		Assert.IsType<OllamaApiClient>(provider.GetRequiredService<IChatClient>());
	}
}
