# Changelog

## [0.6.0] - 2026-11-01

### ✨ New
- **New package: `Moka.AI.McpServer`** — host an MCP server that exposes the tools registered in `AiChatOptions.Tools` to external MCP clients (Claude Desktop, editors, other AI apps). Tools are defined once and shared by the built-in chat pipeline and MCP clients:
  - `McpServerTools.FromAiOptions(options)` converts registered `AIFunction` tools to `McpServerTool`s (the SDK's own conversion; non-function `AITool`s are skipped)
  - `MokaAiMcpServer.RunStdioAsync()` runs the server over stdio — the process mode Claude Desktop launches
  - `MokaAiMcpServer.BuildServerOptions()` builds `McpServerOptions` with all tools attached, for use with any official transport incl. ASP.NET Core HTTP hosting (`AddMcpServer().WithTools(...)` + `MapMcp()`)
  - `AddMokaAiMcpServer()` DI registration
- New "Hosting an MCP Server" guide

## [0.5.0] - 2026-10-31

### ✨ New
- **Tool calling** — `AiChatOptions.Tools` registers `AITool`/`AIFunction` tools (via `AIFunctionFactory.Create`). Tools are sent with every request and the model's calls execute automatically: the chat client is wrapped with `FunctionInvokingChatClient` when `EnableFunctionInvocation` is true (default; set false to bring your own pipeline). Works with both `SendAsync` and `StreamAsync`
- **New package: `Moka.AI.Mcp`** — Model Context Protocol client integration on the official `ModelContextProtocol` 2.x SDK. `AddMokaAiMcp(...)` registers stdio (`npx`-style processes) and HTTP MCP servers; `MokaAiMcpClient.ConnectAsync()` connects during startup, discovers each server's tools, and registers them with the AI pipeline. MCP tools are `McpClientTool`s, which derive from `AIFunction` — zero conversion needed

### 🔧 Changed
- `AiChatOptions.Tools` is app-level shared state by design: the per-instance options snapshot shares the list reference, so tools registered after a service is constructed (e.g. an MCP server connecting post-startup) are visible to every `AiChatService`

## [0.4.0] - 2026-10-30

### ✨ New
- **History trimming** — new `AiChatOptions.MaxHistoryChars` (default 12000 chars, `0` disables) caps how much conversation history is sent. Oldest messages are dropped first, the kept history is re-aligned to start on a user turn, and a truncation note is added to the system prompt so the model knows earlier context was cut. Long conversations no longer grow the prompt unbounded until the provider's context window overflows

### 🐛 Fixed
- `AiChatService`'s per-instance options snapshot now copies `MaxHistoryChars` (and the test suite locks the snapshot contract so new option fields can't be silently missed)

## [0.3.0] - 2026-10-30

### 💥 Breaking Changes
- **New package: `Moka.AI`** — the UI-free chat engine (`AiChatService`, `IAiContextBuilder`, `AiMessage`, `AiChatOptions`, `AddMokaAi()`) extracted from `Moka.Blazor.AI` into its own package with no Blazor or ASP.NET dependency. Types moved from `Moka.Blazor.AI.Models/Services/Extensions` to `Moka.AI.Models/Services/Extensions` — update your `using` directives. `Moka.Blazor.AI` still carries the components and depends on `Moka.AI`
- **Renamed package: `Moka.AI.Onnx`** (was `Moka.Blazor.AI.Onnx`) — the ONNX Runtime GenAI provider now references only `Moka.AI`; namespaces moved from `Moka.Blazor.AI.Onnx.*` to `Moka.AI.Onnx.*`

### ✨ New
- **Headless usage** — console, desktop, mobile, and service apps can now use the AI chat services without any Blazor dependency (see `samples/Moka.AI.ConsoleDemo`)
- **`ApiKey` option** — `AiChatOptions.ApiKey` for providers that require authentication (previously hardcoded to the `"lm-studio"` placeholder, which broke real OpenAI/vLLM endpoints)

### 🐛 Fixed
- **Runtime settings no longer leak across scopes** — `AiChatService` now snapshots its options per instance, so `SetModel`/`SetTemperature`/… in one Blazor circuit (or DI scope) no longer mutate the global options seen by every other user
- **`SetModel()` now takes effect** — the model id is sent with every request, so runtime model changes work even for OpenAI-compatible clients whose underlying chat client was created with a different default model

### 🔧 Changed
- `Moka.Blazor.AI` package now depends on `Moka.AI` (transitively provides the provider packages)

## [0.2.0] - 2026-10-03

### 📦 Dependencies
| Package | From | To |
|---|---|---|
| Microsoft.Extensions.AI.Abstractions | 10.4.1 | 10.10.1 |
| Microsoft.Extensions.AI.OpenAI | 10.4.1 | 10.10.1 |
| OllamaSharp | 5.4.25 | 5.5.0 |
| Markdig | 1.1.2 | 1.4.0 |
| Microsoft.ML.OnnxRuntimeGenAI (Moka.Blazor.AI.Onnx) | 0.12.2 | 0.17.1 |

### 🔧 Changed
- NuGet package pages now show `README.nuget.md`, a shorter readme written for nuget.org, instead of the full GitHub README

## [0.1.0] - 2026-03-27

### ✨ New
- **MokaAiPanel** — reusable Blazor chat panel component with streaming responses, editable messages, and pluggable AI providers
- **Three chat styles** — Bubble (modern), Classic (flat), Compact (minimal) — switchable at runtime from the settings panel
- **Streaming first** — token-by-token streaming via `IAsyncEnumerable` with stop/cancel support
- **Edit & re-send** — edit any previous user message and re-send from that point in the conversation
- **Settings panel** — model, temperature, max context size, streaming toggle, chat style
- **Quick actions** — configurable prompt buttons (e.g., Summarize, Analyze)
- **Markdown rendering** — assistant responses rendered as Markdown via Markdig
- **Token & context tracking** — estimated token count and context window usage
- **Connection status** — auto-detect backend availability with retry
- **Provider support** — OpenAI-compatible (LM Studio, vLLM), Ollama, or any custom `IChatClient`
- **Theming** — light, dark, and auto modes via CSS variables and `[data-moka-ai-theme]` attribute
- **Multi-target** — supports .NET 9 and .NET 10

### ✨ New (Moka.Blazor.AI.Onnx)
- **ONNX Runtime GenAI** companion package for fully embedded/offline AI models
- Registers `OnnxRuntimeGenAIChatClient` as `IChatClient` — no external server required
- Hardware acceleration options: CPU, CUDA, DirectML

[0.6.0]: https://github.com/jacobwi/Moka.Blazor.AI/releases/tag/v0.6.0
[0.5.0]: https://github.com/jacobwi/Moka.Blazor.AI/releases/tag/v0.5.0
[0.4.0]: https://github.com/jacobwi/Moka.Blazor.AI/releases/tag/v0.4.0
[0.3.0]: https://github.com/jacobwi/Moka.Blazor.AI/releases/tag/v0.3.0
[0.2.0]: https://github.com/jacobwi/Moka.Blazor.AI/releases/tag/v0.2.0
[0.1.0]: https://github.com/jacobwi/Moka.Blazor.AI/releases/tag/v0.1.0
