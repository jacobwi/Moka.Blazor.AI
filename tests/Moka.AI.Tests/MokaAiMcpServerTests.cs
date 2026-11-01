using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using Moka.AI.Extensions;
using Moka.AI.McpServer;
using Moka.AI.McpServer.Extensions;
using Moka.AI.Models;
using Xunit;

namespace Moka.AI.Tests;

public sealed class MokaAiMcpServerTests
{
	[Fact]
	public void FromAiOptions_ConvertsAIFunctionTools()
	{
		var options = new AiChatOptions();
		options.Tools.Add(AIFunctionFactory.Create(
			(int a, int b) => a + b,
			new AIFunctionFactoryOptions { Name = "add", Description = "Add two integers." }));

		// The SDK's McpServerTool surface is opaque at compile time (metadata is
		// IReadOnlyList<object>); count-based assertions plus the Create(AIFunction)
		// contract cover the conversion.
		Assert.Single(McpServerTools.FromAiOptions(options));
	}

	[Fact]
	public void FromAiOptions_SkipsNonFunctionTools()
	{
		var options = new AiChatOptions();
		options.Tools.Add(AIFunctionFactory.Create(
			() => "hi",
			new AIFunctionFactoryOptions { Name = "hello" }));
		options.Tools.Add(new OpaqueTool());

		Assert.Single(McpServerTools.FromAiOptions(options));
	}

	[Fact]
	public void FromAiOptions_EmptyTools_YieldsEmptyCollection()
	{
		Assert.Empty(McpServerTools.FromAiOptions(new AiChatOptions()));
	}

	[Fact]
	public async Task AddMokaAiMcpServer_BuildsServerOptionsWithRegisteredTools()
	{
		var services = new ServiceCollection();
		services.AddMokaAi(options => options.Tools.Add(AIFunctionFactory.Create(
			(string city) => city.ToUpperInvariant(),
			new AIFunctionFactoryOptions { Name = "shout" })));
		services.AddMokaAiMcpServer();

		await using var provider = services.BuildServiceProvider();
		MokaAiMcpServer host = provider.GetRequiredService<MokaAiMcpServer>();

		McpServerOptions serverOptions = host.BuildServerOptions();

		Assert.NotNull(serverOptions.ToolCollection);
		Assert.Single(serverOptions.ToolCollection);
		Assert.Equal("MokaAI", serverOptions.ServerInfo?.Name);
	}

	private sealed class OpaqueTool : AITool
	{
		public override string Name => "opaque";
	}
}
