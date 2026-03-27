---
title: "Moka.Blazor.AI"
---

# Moka.Blazor.AI

A reusable Blazor AI chat panel component with streaming responses, editable messages, markdown rendering, and pluggable AI providers.

## Get Started

```bash
dotnet add package Moka.Blazor.AI
```

```csharp
// Program.cs
builder.Services.AddMokaAi(options =>
{
    options.Provider = AiProvider.OpenAiCompatible;
    options.Endpoint = "http://localhost:1234";
    options.DefaultModel = "qwen2.5-3b";
});
```

```razor
@using Moka.Blazor.AI.Components

<MokaAiPanel Title="AI Assistant" />
```

## Key Features

- **Streaming responses** — token-by-token streaming with typing indicator and stop/cancel
- **Edit & re-send** — edit any previous user message and re-send from that point
- **Three chat styles** — Bubble (modern), Classic (flat), Compact (minimal)
- **Quick actions** — configurable prompt buttons for common tasks
- **Markdown rendering** — assistant responses rendered as Markdown via Markdig
- **Settings panel** — model, temperature, max context, streaming, chat style
- **Pluggable providers** — OpenAI-compatible, Ollama, ONNX Runtime, or any `IChatClient`
- **Context builder** — `IAiContextBuilder` for domain-specific context injection
- **Theming** — light, dark, and auto modes via CSS variables

## Packages

| Package | Description |
|---------|-------------|
| [Moka.Blazor.AI](https://www.nuget.org/packages/Moka.Blazor.AI) | Core chat panel component and services |
| [Moka.Blazor.AI.Onnx](https://www.nuget.org/packages/Moka.Blazor.AI.Onnx) | ONNX Runtime GenAI provider for embedded/offline models |

## Next Steps

- [Getting Started](guide/getting-started.md) — installation, setup, and provider configuration
- [Configuration](guide/configuration.md) — options, quick actions, and context builders
- [Theming](guide/theming.md) — built-in themes and CSS variable customization
- [API Reference](/api) — full API documentation
