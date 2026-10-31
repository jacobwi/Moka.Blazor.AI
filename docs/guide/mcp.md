---
title: MCP & Tool Calling
---

# MCP & Tool Calling

Moka.AI supports two ways to give the AI tools:

1. **AIFunction tools** — register .NET delegates directly via `AIFunctionFactory`
2. **MCP servers** — connect [Model Context Protocol](https://modelcontextprotocol.io) servers (local processes or remote HTTP) with the `Moka.AI.Mcp` package

Both register into `AiChatOptions.Tools`. When tools are present, the chat client is wrapped with `FunctionInvokingChatClient` (enabled by default via `AiChatOptions.EnableFunctionInvocation`) so the model's tool calls execute automatically: the model requests a call, Moka.AI runs it, sends the result back, and the model continues — possibly over several rounds — before answering.

## AIFunction Tools

```csharp
using Microsoft.Extensions.AI;
using Moka.AI.Extensions;
using Moka.AI.Models;

builder.Services.AddMokaAi(options =>
{
    options.DefaultModel = "qwen2.5-3b";

    options.Tools.Add(AIFunctionFactory.Create(
        (string city) => WeatherService.Current(city),
        new AIFunctionFactoryOptions
        {
            Name = "get_weather",
            Description = "Get the current weather for a city."
        }));
});
```

No other wiring is needed — tools are sent with every request and their results are looped back automatically.

## MCP Servers

Install the companion package:

```bash
dotnet add package Moka.AI.Mcp
```

Register the servers you want to connect:

```csharp
using Moka.AI.Mcp;
using Moka.AI.Mcp.Extensions;

builder.Services.AddMokaAi(options => { /* provider config */ });
builder.Services.AddMokaAiMcp(mcp => mcp
    .AddStdioServer("filesystem", "npx", "-y", "@modelcontextprotocol/server-filesystem", @"C:\data")
    .AddHttpServer("github", new Uri("http://localhost:7801/mcp")));
```

Then connect once during startup, **before the first chat request**:

```csharp
// Program.cs — after builder.Build()
await app.Services.GetRequiredService<MokaAiMcpClient>().ConnectAsync();
```

`ConnectAsync` spawns/connects each server, discovers its tools, and registers them with the shared `AiChatOptions.Tools` list. A shorthand overload registers a single stdio server:

```csharp
builder.Services.AddMokaAiMcp("filesystem", "npx", "-y", "@modelcontextprotocol/server-filesystem", @"C:\data");
```

| Member | Description |
|--------|-------------|
| `MokaAiMcpClient.IsConnected` | Whether `ConnectAsync` has completed |
| `MokaAiMcpClient.ServerNames` | Names of the configured servers |
| `MokaAiMcpClient.ToolNames` | Tools discovered across all servers |

## Notes & Caveats

- **Model support** — the model must support tool/function calling (most modern models do: Qwen 2.5, Llama 3.x, Phi-3.5+, GPT-4x, Claude)
- **Shared tool list** — `AiChatOptions.Tools` is app-level shared state; tools registered after services are constructed are still visible to every `AiChatService` instance
- **Bring your own pipeline** — if you wrap your own `FunctionInvokingChatClient`, set `options.EnableFunctionInvocation = false` to avoid double wrapping
- **Server lifetime** — `MokaAiMcpClient` is a singleton and disposes all connected servers when the service provider is disposed
- **Streaming** — tool calls work with both `SendAsync` and `StreamAsync`; the streamed text you see is the model's final answer after tool rounds complete
