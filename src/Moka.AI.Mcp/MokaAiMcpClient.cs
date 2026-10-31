using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using Moka.AI.Models;

namespace Moka.AI.Mcp;

/// <summary>
///     Connects the configured MCP servers and registers their tools with
///     <see cref="AiChatOptions.Tools" /> so the AI can call them. Call
///     <see cref="ConnectAsync" /> once during startup, before the first chat request.
/// </summary>
public sealed class MokaAiMcpClient(
	IOptions<AiChatOptions> aiOptions,
	IOptions<Mcp.Models.MokaAiMcpOptions> mcpOptions,
	ILoggerFactory? loggerFactory = null) : IAsyncDisposable
{
	private readonly List<McpClient> _clients = [];

	/// <summary>Whether <see cref="ConnectAsync" /> has completed successfully.</summary>
	public bool IsConnected { get; private set; }

	/// <summary>Names of the configured MCP servers.</summary>
	public IReadOnlyList<string> ServerNames =>
		mcpOptions.Value.Servers.Select(s => s.Name).ToList();

	/// <summary>Names of the tools discovered across all connected servers.</summary>
	public IReadOnlyList<string> ToolNames { get; private set; } = [];

	/// <summary>
	///     Connects every configured MCP server, discovers its tools, and registers them
	///     with the shared AI options. The tool list is shared by reference with all
	///     <see cref="Services.AiChatService" /> instances, so this must run before the
	///     first chat request for tools to be visible (unless connected later, which
	///     affects services created after the connection).
	/// </summary>
	public async Task ConnectAsync(CancellationToken cancellationToken = default)
	{
		if (IsConnected)
		{
			return;
		}

		var toolNames = new List<string>();

		foreach (Mcp.Models.McpServerDefinition server in mcpOptions.Value.Servers)
		{
			// The McpClient session created below owns the transport and disposes it
			// together with the session — do not dispose it here.
#pragma warning disable CA2000 // Dispose objects before losing scope
			IClientTransport transport = server.Endpoint is not null
				? new HttpClientTransport(new HttpClientTransportOptions
				{
					Name = server.Name,
					Endpoint = server.Endpoint
				})
				: new StdioClientTransport(new StdioClientTransportOptions
				{
					Name = server.Name,
					Command = server.Command ?? throw new InvalidOperationException(
						$"MCP server '{server.Name}' has neither a command nor an endpoint."),
					Arguments = server.Arguments?.ToList(),
					WorkingDirectory = server.WorkingDirectory
				});
#pragma warning restore CA2000 // Dispose objects before losing scope

			McpClient client = await McpClient.CreateAsync(transport, new McpClientOptions(), loggerFactory, cancellationToken);
			_clients.Add(client);

			// McpClientTool derives from AIFunction — usable directly in ChatOptions.Tools
			IList<McpClientTool> tools = await client.ListToolsAsync(cancellationToken: cancellationToken);

			foreach (McpClientTool tool in tools)
			{
				aiOptions.Value.Tools.Add(tool);
				toolNames.Add(tool.Name);
			}
		}

		ToolNames = toolNames;
		IsConnected = true;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		foreach (McpClient client in _clients)
		{
			await client.DisposeAsync();
		}

		_clients.Clear();
	}
}
