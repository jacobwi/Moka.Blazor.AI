using Microsoft.Extensions.DependencyInjection;
using Moka.AI.Extensions;
using Moka.AI.Mcp;
using Moka.AI.Mcp.Extensions;
using Moka.AI.Mcp.Models;
using Xunit;

namespace Moka.AI.Tests;

public sealed class MokaAiMcpTests
{
	[Fact]
	public async Task AddMokaAiMcp_RegistersSingletonClientWithConfiguredServers()
	{
		var services = new ServiceCollection();
		services.AddMokaAi(_ => { });
		services.AddMokaAiMcp(mcp => mcp.AddStdioServer("fs", "npx", "-y", "@modelcontextprotocol/server-filesystem"));

		await using var provider = services.BuildServiceProvider();
		MokaAiMcpClient client = provider.GetRequiredService<MokaAiMcpClient>();
		Assert.False(client.IsConnected);
		Assert.Equal(["fs"], client.ServerNames);
	}

	[Fact]
	public void McpOptions_CapturesStdioAndHttpServers()
	{
		var options = new MokaAiMcpOptions()
			.AddStdioServer("a", "npx", "-y", "pkg")
			.AddHttpServer("b", new Uri("http://localhost:7801/mcp"));

		Assert.Equal(2, options.Servers.Count);
		Assert.Equal("npx", options.Servers[0].Command);
		Assert.Equal(["-y", "pkg"], options.Servers[0].Arguments);
		Assert.Null(options.Servers[0].Endpoint);
		Assert.Equal(new Uri("http://localhost:7801/mcp"), options.Servers[1].Endpoint);
		Assert.Null(options.Servers[1].Command);
	}

	[Fact]
	public async Task AddMokaAiMcp_ShortOverload_RegistersSingleServer()
	{
		var services = new ServiceCollection();
		services.AddMokaAi(_ => { });
		services.AddMokaAiMcp("fs", "npx", "-y", "pkg");

		await using var provider = services.BuildServiceProvider();
		Assert.Equal(["fs"], provider.GetRequiredService<MokaAiMcpClient>().ServerNames);
	}
}
