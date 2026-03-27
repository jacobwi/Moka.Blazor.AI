---
title: Getting Started
---

# Getting Started

## Installation

```bash
dotnet add package Moka.Blazor.AI
```

For embedded ONNX models (no external server):

```bash
dotnet add package Moka.Blazor.AI.Onnx
```

## Service Registration

Register the AI services in `Program.cs`:

```csharp
using Moka.Blazor.AI.Extensions;
using Moka.Blazor.AI.Models;

builder.Services.AddMokaAi(options =>
{
    options.Provider = AiProvider.OpenAiCompatible;
    options.Endpoint = "http://localhost:1234";
    options.DefaultModel = "qwen2.5-3b";
});
```

## Basic Usage

```razor
@using Moka.Blazor.AI.Components

<MokaAiPanel Title="My Assistant"
             SystemPrompt="You are a helpful coding assistant."
             MessagesHeight="500px" />
```

## Provider Configuration

### OpenAI-Compatible (LM Studio, vLLM, text-generation-webui)

```csharp
builder.Services.AddMokaAi(options =>
{
    options.Provider = AiProvider.OpenAiCompatible;
    options.Endpoint = "http://localhost:1234";
    options.DefaultModel = "qwen2.5-3b";
    options.Temperature = 0.3f;
});
```

### Ollama

```csharp
builder.Services.AddMokaAi(options =>
{
    options.Provider = AiProvider.Ollama;
    options.Endpoint = "http://localhost:11434";
    options.DefaultModel = "llama3.2";
});
```

### ONNX Runtime GenAI (Embedded)

```csharp
builder.Services.AddMokaAiOnnx(@"C:\models\phi-3-mini-onnx");
```

No external server required — the model runs fully in-process.

### Custom IChatClient

```csharp
IChatClient myClient = /* your custom implementation */;
builder.Services.AddMokaAi(myClient);
```

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Title` | `string` | `"AI Assistant"` | Panel header text |
| `SystemPrompt` | `string` | `"You are a helpful..."` | System prompt for the AI |
| `QuickActions` | `IReadOnlyList<AiQuickAction>` | `null` | Quick action prompt buttons |
| `ActionsExtra` | `RenderFragment` | `null` | Custom toolbar content via `<ActionsExtra>` |
| `Placeholder` | `string` | `"Ask a question..."` | Input placeholder text |
| `MessagesHeight` | `string` | `"350px"` | Messages area height |
| `ShowQuickActions` | `bool` | `true` | Show/hide quick action buttons |
| `ChatStyle` | `ChatStyle` | `Bubble` | Visual style (Bubble, Classic, Compact) |
| `ThemeAttribute` | `string` | `""` | Theme (`"light"`, `"dark"`, `""`) |

## Event Callbacks

The panel exposes public methods for programmatic access via `@ref`:

```razor
<MokaAiPanel @ref="_panel" />

@code {
    private MokaAiPanel _panel = null!;

    private async Task AskQuestion()
    {
        await _panel.SendToAi("What is the meaning of life?");
    }
}
```

| Member | Type | Description |
|--------|------|-------------|
| `SendToAi(string text)` | `Task` | Send a message and stream the AI response |
| `IsSending` | `bool` | Whether a message is currently being processed |
| `Messages` | `IReadOnlyList<AiMessage>` | The conversation history |

### IAiContextBuilder (Scoping API)

When implementing a custom context builder, the following default interface methods are available for scoping:

| Method | Description |
|--------|-------------|
| `SetScope(string key, object? data)` | Set a named scope to narrow context (e.g., `"path"` for a subtree) |
| `ClearScope(string? key)` | Clear a specific scope, or all scopes if `key` is `null` |
| `GetScopes()` | Returns all active scopes as `IReadOnlyDictionary<string, object?>` |

These have default no-op implementations — existing builders work without changes. See [Extending](/guide/extending) for details.

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| `Enter` | Send message |
| `Shift+Enter` | New line in message |
| `Escape` | Cancel edit mode |
