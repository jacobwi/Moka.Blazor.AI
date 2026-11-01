using System.Collections.ObjectModel;
using Microsoft.Extensions.AI;
using Moka.AI.Models;
using ModelContextProtocol.Server;

namespace Moka.AI.McpServer;

/// <summary>
///     Converts the <see cref="AITool" />s registered in <see cref="AiChatOptions.Tools" />
///     into <see cref="McpServerTool" />s so the same tools serve both the built-in AI
///     chat pipeline and external MCP clients.
/// </summary>
public static class McpServerTools
{
	/// <summary>
	///     Converts all <see cref="AIFunction" /> tools registered on the given options.
	///     Non-function <see cref="AITool" /> implementations are skipped — MCP requires
	///     callable functions with JSON schemas, which only <see cref="AIFunction" /> provides.
	/// </summary>
	public static Collection<McpServerTool> FromAiOptions(AiChatOptions options) =>
		new([.. options.Tools
			.OfType<AIFunction>()
			.Select(t => McpServerTool.Create(t, new McpServerToolCreateOptions()))]);
}
