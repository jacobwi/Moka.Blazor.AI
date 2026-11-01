using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moka.AI.McpServer;

#pragma warning disable CA1062 // Validate arguments of public methods

namespace Moka.AI.McpServer.Extensions;

/// <summary>
///     Extension methods for registering the Moka.AI MCP server host.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///     Registers <see cref="MokaAiMcpServer" />, which exposes the tools registered in
	///     <see cref="Moka.AI.Models.AiChatOptions.Tools" /> to external MCP clients.
	///     Resolve it and call <see cref="MokaAiMcpServer.RunStdioAsync" /> from a console
	///     entry point, or use <see cref="McpServerTools.FromAiOptions" /> with the official
	///     ASP.NET Core HTTP hosting.
	/// </summary>
	public static IServiceCollection AddMokaAiMcpServer(this IServiceCollection services)
	{
		services.TryAddSingleton<MokaAiMcpServer>();
		return services;
	}
}
