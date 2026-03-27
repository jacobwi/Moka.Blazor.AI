---
title: Configuration
---

# Configuration

## AiChatOptions

Configure the AI chat service via `AddMokaAi()`:

```csharp
builder.Services.AddMokaAi(options =>
{
    options.Provider = AiProvider.OpenAiCompatible;
    options.Endpoint = "http://localhost:1234";
    options.DefaultModel = "qwen2.5-3b";
    options.Temperature = 0.3f;
    options.MaxContextChars = 8000;
    options.StreamResponses = true;
});
```

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Provider` | `AiProvider` | `OpenAiCompatible` | AI provider type |
| `Endpoint` | `string?` | `null` | Provider endpoint URL (uses provider default if null) |
| `DefaultModel` | `string?` | `null` | Model name to use |
| `Temperature` | `float` | `0.3` | Response randomness (0.0 = deterministic, 1.0 = creative) |
| `MaxContextChars` | `int` | `8000` | Maximum characters included as context |
| `StreamResponses` | `bool` | `true` | Enable token-by-token streaming |
| `SystemPrompt` | `string?` | `null` | Default system prompt |

## AI Providers

| Provider | Endpoint Default | Description |
|----------|-----------------|-------------|
| `OpenAiCompatible` | `http://localhost:1234/v1` | LM Studio, vLLM, text-generation-webui, or any OpenAI-compatible API |
| `Ollama` | `http://localhost:11434` | Ollama native API |
| `Onnx` | N/A (in-process) | ONNX Runtime GenAI — requires `Moka.Blazor.AI.Onnx` package |

## Quick Actions

Add custom quick action buttons to the panel:

```razor
@using Moka.Blazor.AI.Models

<MokaAiPanel QuickActions="_quickActions" />

@code {
    private static readonly IReadOnlyList<AiQuickAction> _quickActions =
    [
        new("Summarize", "Summarize this content.", "Summarize"),
        new("Explain", "Explain this in simple terms.", "Explain"),
        new("Review", "Review this for issues.", "Review code")
    ];
}
```

Each `AiQuickAction` takes:
- **Label** — button display text
- **Prompt** — the prompt sent to the AI when clicked
- **Description** — tooltip text

## Chat Styles

Three visual styles available, switchable at runtime from the settings panel:

| Style | Description |
|-------|-------------|
| **Bubble** | Modern messaging with rounded bubbles, avatars, and gradient send button |
| **Classic** | Flat messages with role labels and subtle borders |
| **Compact** | Minimal layout optimized for side panels and small spaces |

```razor
<MokaAiPanel ChatStyle="ChatStyle.Compact" />
```

## Context Builder

Implement `IAiContextBuilder` to inject domain-specific context into every AI prompt:

```csharp
public class MyContextBuilder : IAiContextBuilder
{
    public string BuildContext(AiChatOptions options)
    {
        // Return relevant context for the AI
        return "The user is working on project X with 15 open tasks.";
    }
}
```

Register it in DI:

```csharp
builder.Services.AddScoped<IAiContextBuilder, MyContextBuilder>();
```

The `AiChatService` automatically calls `BuildContext()` and prepends the result to each prompt as context.

### Scoping

The context builder supports **scoping** — narrowing context to specific data before sending a message. This is useful when working with large data sources where you only want the AI to see a relevant subset.

```csharp
// Focus context on a specific item
builder.SetScope("path", "/users/0");

// Or provide multiple named data sources for comparison
builder.SetScope("sources", new Dictionary<string, string>
{
    ["Customer"] = customerJson,
    ["Order"] = orderJson
});

// Clear all scopes
builder.ClearScope();
```

The `SetScope`, `ClearScope`, and `GetScopes` methods have default no-op implementations on the interface, so existing builders continue to work without changes. See the [Extending guide](/guide/extending) for full implementation details.

## Settings Panel

The built-in settings panel (gear icon) allows users to configure at runtime:

- **Model** — change the active model name
- **Chat Style** — switch between Bubble, Classic, Compact
- **Temperature** — adjust response creativity (0.0–1.0)
- **Max Context** — limit context window size
- **Stream Responses** — toggle streaming on/off

All settings changes take effect immediately for the next message.
