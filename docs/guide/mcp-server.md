---
title: Hosting an MCP Server
---

# Hosting an MCP Server

Flip the direction: instead of your app calling MCP servers, **external MCP clients** (Claude Desktop, editors, other AI apps) call **your** tools. The tools registered in `AiChatOptions.Tools` are defined once and serve both sides — the built-in chat pipeline and any MCP client.

```bash
dotnet add package Moka.AI.McpServer
```

## Console App (stdio) — the Claude Desktop mode

This is the process mode MCP clients launch directly:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moka.AI.Extensions;
using Moka.AI.McpServer;
using Moka.AI.McpServer.Extensions;
using Moka.AI.Models;

var services = new ServiceCollection();
services.AddLogging(l => l.AddConsole()); // stderr by default — stdout belongs to the protocol
services.AddMokaAi(options =>
{
    options.Tools.Add(AIFunctionFactory.Create(
        (string path) => File.ReadAllText(path),
        new AIFunctionFactoryOptions { Name = "read_file", Description = "Read a text file." }));
});
services.AddMokaAiMcpServer();

await using var provider = services.BuildServiceProvider();
await provider.GetRequiredService<MokaAiMcpServer>().RunStdioAsync("my-app");
```

Then register it with Claude Desktop (`claude_desktop_config.json`):

```json
{
  "mcpServers": {
    "my-app": {
      "command": "C:\\path\\to\\my-app.exe"
    }
  }
}
```

## ASP.NET Core (HTTP)

For remote hosting, use the official `ModelContextProtocol.AspNetCore` package and feed it the converted tools — no Moka wrapper needed on the transport:

```bash
dotnet add package ModelContextProtocol.AspNetCore
```

```csharp
using Moka.AI.McpServer;

builder.Services.AddMokaAi(options => { /* provider config + tools */ });
builder.Services.AddMokaAiMcpServer();

builder.Services.AddMcpServer()
    .WithTools(McpServerTools.FromAiOptions(
        builder.Configuration.GetSection("Ai").Get<AiChatOptions>() ?? new AiChatOptions()));
    // or resolve from DI at runtime via a factory

app.MapMcp();
```

`McpServerTools.FromAiOptions` converts every registered `AIFunction` into an `McpServerTool` (the SDK's own conversion — schemas included).

## API Surface

| Member | Description |
|--------|-------------|
| `McpServerTools.FromAiOptions(AiChatOptions)` | Converts registered `AIFunction` tools to `McpServerTool`s (non-function `AITool`s are skipped) |
| `MokaAiMcpServer.BuildServerOptions()` | Builds `McpServerOptions` with all tools attached and `ServerInfo = MokaAI` |
| `MokaAiMcpServer.RunStdioAsync(name, ct)` | Runs the server over stdio until cancelled |
| `AddMokaAiMcpServer()` | Registers `MokaAiMcpServer` as a singleton |

## Notes

- **Dual-use tools** — the same `options.Tools` list drives the built-in chat pipeline and the MCP server; registering more tools extends both
- **stdio is the protocol** — when running `RunStdioAsync`, never write to stdout; keep diagnostics on stderr (`AddConsole` defaults there)
- **Browser-state caveat** — for Blazor viewers (e.g. Moka.Blazor.Json), the MCP server can only expose data reachable from the process it runs in; register tools that fetch documents server-side, the same pattern as a context builder
