# Moka.Blazor.AI

A reusable Blazor AI chat panel component with streaming responses, editable messages, markdown rendering, and pluggable AI providers.

## Features

- **Streaming responses** — token-by-token streaming with typing indicator and stop/cancel
- **Edit & re-send** — edit any previous message and re-send from that point
- **Three chat styles** — Bubble, Classic, Compact — switchable at runtime
- **Quick actions** — configurable prompt buttons for common tasks
- **Markdown rendering** — assistant responses rendered as Markdown
- **Settings panel** — model, temperature, max context, streaming toggle
- **Theming** — light, dark, and auto modes via CSS variables
- **Pluggable providers** — OpenAI-compatible, Ollama, ONNX Runtime, or custom `IChatClient`
- **Multi-target** — .NET 9 and .NET 10

## Quick Start

Register services in `Program.cs`:

```csharp
builder.Services.AddMokaAi(options =>
{
    options.Provider = AiProvider.OpenAiCompatible;
    options.Endpoint = "http://localhost:1234";
    options.DefaultModel = "qwen2.5-3b";
});
```

Add the component:

```razor
@using Moka.Blazor.AI.Components

<MokaAiPanel Title="AI Assistant" />
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` | `"AI Assistant"` | Panel header text |
| `SystemPrompt` | `string` | `"You are a helpful..."` | System prompt for the AI |
| `QuickActions` | `IReadOnlyList<AiQuickAction>` | `null` | Quick action prompt buttons |
| `Placeholder` | `string` | `"Ask a question..."` | Input placeholder text |
| `MessagesHeight` | `string` | `"350px"` | Messages area height |
| `ChatStyle` | `ChatStyle` | `Bubble` | Visual style (Bubble, Classic, Compact) |
| `ThemeAttribute` | `string` | `""` | Theme (`"light"`, `"dark"`, `""`) |

## Related Packages

| Package | Description |
|---------|-------------|
| [Moka.Blazor.AI.Onnx](https://www.nuget.org/packages/Moka.Blazor.AI.Onnx) | ONNX Runtime GenAI provider for embedded/offline models |
| [Moka.Blazor.Json.AI](https://www.nuget.org/packages/Moka.Blazor.Json.AI) | JSON-specific AI panel for the Moka.Blazor.Json viewer |

## Documentation

Full documentation at [jacobwi.github.io/Moka.Blazor.AI](https://jacobwi.github.io/Moka.Blazor.AI/)

## License

MIT
