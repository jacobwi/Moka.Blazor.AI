using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moka.AI.Mcp;
using Moka.AI.Mcp.Models;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Moka.AI.Mcp.Extensions;

/// <summary>
///     Extension methods for registering MCP (Model Context Protocol) support with Moka.AI.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers the MCP client with the servers configured via <paramref name="configure" />.
	///     Resolve <see cref="MokaAiMcpClient" /> and await <see cref="MokaAiMcpClient.ConnectAsync" />
	///     during startup (before the first chat request) to spawn the servers and register their tools.
	/// </summary>
	public static IServiceCollection AddMokaAiMcp(
		this IServiceCollection services,
		Action<MokaAiMcpOptions>? configure = null)
	{
		services.Configure(configure ?? (_ => { }));
		services.TryAddSingleton<MokaAiMcpClient>();
		return services;
	}

	/// <summary>
	///     Registers the MCP client with a single stdio server. Resolve
	///     <see cref="MokaAiMcpClient" /> and await <see cref="MokaAiMcpClient.ConnectAsync" />
	///     during startup to spawn the server and register its tools.
	/// </summary>
	/// <param name="services">The service collection.</param>
	/// <param name="name">Display name for the server.</param>
	/// <param name="command">Executable to launch (e.g. <c>npx</c>).</param>
	/// <param name="arguments">Arguments for the executable (e.g. <c>-y</c>, <c>@modelcontextprotocol/server-filesystem</c>).</param>
	public static IServiceCollection AddMokaAiMcp(
		this IServiceCollection services,
		string name,
		string command,
		params string[] arguments)
		=> services.AddMokaAiMcp(mcp => mcp.AddStdioServer(name, command, arguments));
}
