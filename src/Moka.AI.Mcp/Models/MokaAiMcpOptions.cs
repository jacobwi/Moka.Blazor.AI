using System.Collections.ObjectModel;

namespace Moka.AI.Mcp.Models;

/// <summary>
///     Configuration for MCP servers connected via <see cref="MokaAiMcpClient" />.
/// </summary>
public sealed class MokaAiMcpOptions
{
	/// <summary>MCP servers to connect. A server is stdio-based when <see cref="McpServerDefinition.Command" /> is set, HTTP-based when <see cref="McpServerDefinition.Endpoint" /> is set.</summary>
	public Collection<McpServerDefinition> Servers { get; } = [];

	/// <summary>Adds a stdio MCP server (a local process, e.g. <c>npx -y @modelcontextprotocol/server-filesystem</c>).</summary>
	public MokaAiMcpOptions AddStdioServer(string name, string command, params string[] arguments)
	{
		Servers.Add(new McpServerDefinition(name, command, arguments));
		return this;
	}

	/// <summary>Adds a remote HTTP (streamable HTTP / SSE) MCP server.</summary>
	public MokaAiMcpOptions AddHttpServer(string name, Uri endpoint)
	{
		Servers.Add(new McpServerDefinition(name, Endpoint: endpoint));
		return this;
	}
}

/// <summary>
///     A single MCP server definition. Stdio when <paramref name="command" /> is given,
///     HTTP when <paramref name="endpoint" /> is given.
/// </summary>
/// <param name="Name">Display name for the server.</param>
/// <param name="Command">Executable to launch (stdio transport). Example: <c>npx</c>.</param>
/// <param name="Arguments">Arguments passed to the executable.</param>
/// <param name="WorkingDirectory">Optional working directory for the stdio process.</param>
/// <param name="Endpoint">HTTP endpoint of a remote MCP server (HTTP transport).</param>
public sealed record McpServerDefinition(
	string Name,
	string? Command = null,
	IReadOnlyList<string>? Arguments = null,
	string? WorkingDirectory = null,
	Uri? Endpoint = null);
