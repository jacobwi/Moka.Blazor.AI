using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moka.AI.Models;

// Alias: the surrounding Moka.AI.McpServer namespace would otherwise shadow the SDK's McpServer class
using McpServerHost = ModelContextProtocol.Server.McpServer;

namespace Moka.AI.McpServer;

/// <summary>
///     Hosts an MCP server that exposes the tools registered in
///     <see cref="AiChatOptions.Tools" /> to external MCP clients
///     (Claude Desktop, editors, other AI apps). Define tools once —
///     the built-in AI chat pipeline and MCP clients share them.
/// </summary>
public sealed class MokaAiMcpServer(
	IOptions<AiChatOptions> aiOptions,
	ILoggerFactory? loggerFactory = null)
{
	/// <summary>Builds <see cref="McpServerOptions" /> with every registered AIFunction tool attached.</summary>
	public McpServerOptions BuildServerOptions()
	{
		// ToolCollection is not initialized by the default options constructor —
		// the SDK expects it to be assigned.
		var toolCollection = new McpServerPrimitiveCollection<McpServerTool>();

		foreach (McpServerTool tool in McpServerTools.FromAiOptions(aiOptions.Value))
		{
			toolCollection.Add(tool);
		}

		return new McpServerOptions
		{
			ServerInfo = new Implementation
			{
				Name = "MokaAI",
				Version = typeof(MokaAiMcpServer).Assembly.GetName()?.Version?.ToString(3) ?? "1.0.0"
			},
			ToolCollection = toolCollection
		};
	}

	/// <summary>
	///     Runs the MCP server over standard input/output until cancelled. Call this from a
	///     console app's entry point — this is the process mode MCP clients such as Claude
	///     Desktop launch directly. Remember: stdin/stdout belong to the protocol, so keep
	///     diagnostics on stderr.
	/// </summary>
	/// <param name="serverName">Server name used in MCP initialization.</param>
	/// <param name="cancellationToken">Stops the server when cancelled.</param>
	public async Task RunStdioAsync(
		string serverName = "moka-ai",
		CancellationToken cancellationToken = default)
	{
		await using StdioServerTransport transport = new(serverName, loggerFactory);
		await using McpServerHost server = McpServerHost.Create(transport, BuildServerOptions(), loggerFactory);
		await server.RunAsync(cancellationToken);
	}
}
