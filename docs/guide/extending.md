---
title: Extending
---

# Extending Moka.Blazor.AI

Moka.Blazor.AI is designed to be extended for domain-specific use cases. This guide walks through how to build your own AI panel on top of the base library — using the real-world `Moka.Blazor.Json.AI` package as a reference.

## Architecture Overview

The extension pattern has three layers:

1. **Context Builder** — implements `IAiContextBuilder` to inject domain-specific data into prompts
2. **Wrapper Component** — composes `MokaAiPanel` via `@ref` and adds domain-specific UI (buttons, quick actions)
3. **DI Registration** — extension method that calls `AddMokaAi()` then registers your services

## Step 1: Implement IAiContextBuilder

The context builder extracts relevant data from your domain and includes it in every AI prompt. The AI model receives this context alongside the user's question.

```csharp
using Moka.Blazor.AI.Models;
using Moka.Blazor.AI.Services;

namespace MyApp.AI.Services;

internal sealed class MyContextBuilder : IAiContextBuilder
{
    private MyDataSource? _dataSource;

    public string BuildContext(AiChatOptions options)
    {
        if (_dataSource is null)
            return "[No data source connected]";

        string data = _dataSource.GetCurrentData();

        // Truncate if too large for the context window
        if (data.Length > options.MaxContextChars)
            return data[..options.MaxContextChars] + "\n...";

        return data;
    }

    internal void SetDataSource(MyDataSource? source) => _dataSource = source;
}
```

Key points:
- Use `options.MaxContextChars` to respect the user's context size setting
- Return a descriptive message when no data is available
- The builder is **scoped** so the same instance is shared within a Blazor circuit

## Step 2: Create a Wrapper Component

Use **composition** (not inheritance) to wrap `MokaAiPanel`. Hold a `@ref` to the base panel and delegate to its public methods.

### Razor markup

```razor
@namespace MyApp.AI.Components

<MokaAiPanel @ref="_panel"
             Title="@Title"
             SystemPrompt="@_systemPrompt"
             QuickActions="@_quickActions"
             MessagesHeight="@MessagesHeight"
             Placeholder="@Placeholder"
             ShowQuickActions="ShowQuickActions"
             ThemeAttribute="@ThemeAttribute">
    <ActionsExtra>
        @* Add domain-specific action buttons *@
        <button @onclick="AnalyzeSelection"
                disabled="@(_panel?.IsSending ?? true)">
            Analyze
        </button>
    </ActionsExtra>
</MokaAiPanel>
```

### Code-behind

```csharp
using Microsoft.AspNetCore.Components;
using Moka.Blazor.AI.Components;
using Moka.Blazor.AI.Models;
using MyApp.AI.Services;

namespace MyApp.AI.Components;

public sealed partial class MyAiPanel : ComponentBase
{
    private const string _systemPrompt = """
        You are a domain-specific assistant.
        The user has data loaded and will ask questions about it.
        Be concise and direct.
        """;

    private static readonly IReadOnlyList<AiQuickAction> _quickActions =
    [
        new("Summarize", "Summarize the data.", "Summarize"),
        new("Analyze", "Find issues.", "Analyze for problems"),
        new("Export", "Suggest export format.", "Recommend format")
    ];

    private MokaAiPanel? _panel;

    [Inject] private MyContextBuilder ContextBuilder { get; set; } = null!;

    [Parameter] public MyDataSource? DataSource { get; set; }
    [Parameter] public string Title { get; set; } = "AI Assistant";
    [Parameter] public string Placeholder { get; set; } = "Ask a question...";
    [Parameter] public string MessagesHeight { get; set; } = "350px";
    [Parameter] public bool ShowQuickActions { get; set; } = true;
    [Parameter] public string ThemeAttribute { get; set; } = "";

    protected override void OnParametersSet()
    {
        // Keep the context builder in sync
        ContextBuilder.SetDataSource(DataSource);
    }

    /// <summary>
    ///     Send a custom prompt to the AI.
    /// </summary>
    public async Task AskAbout(string topic)
    {
        if (_panel is null || _panel.IsSending)
            return;

        await _panel.SendToAi($"Analyze: {topic}");
    }

    private async Task AnalyzeSelection()
    {
        if (_panel is null || _panel.IsSending)
            return;

        await _panel.SendToAi("Analyze the current data for issues and anomalies.");
    }
}
```

## Step 3: Register Services

Create an extension method that calls `AddMokaAi()` from the base library, then registers your domain-specific services:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moka.Blazor.AI.Extensions;
using Moka.Blazor.AI.Models;
using Moka.Blazor.AI.Services;
using MyApp.AI.Services;

namespace MyApp.AI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMyAi(this IServiceCollection services)
        => services.AddMyAi(_ => { });

    public static IServiceCollection AddMyAi(
        this IServiceCollection services,
        Action<AiChatOptions> configure)
    {
        // Register base AI services (IChatClient, AiChatService)
        services.AddMokaAi(configure);

        // Register domain-specific services as Scoped
        // (critical: the same instance must be shared within a circuit)
        services.TryAddScoped<MyContextBuilder>();

        // Wire up as the IAiContextBuilder for the base library
        services.TryAddScoped<IAiContextBuilder>(
            sp => sp.GetRequiredService<MyContextBuilder>());

        return services;
    }
}
```

**Important:** Register your context builder as **Scoped**, not Transient. The wrapper component calls `SetDataSource()` on one instance — if the `AiChatService` resolves a different instance via DI, it won't have the data source reference.

## Step 4: Use It

```csharp
// Program.cs
builder.Services.AddMyAi(options =>
{
    options.Provider = AiProvider.OpenAiCompatible;
    options.Endpoint = "http://localhost:1234";
    options.DefaultModel = "qwen2.5-3b";
});
```

```razor
<MyDataViewer @ref="_viewer" />
<MyAiPanel DataSource="_viewer" />

@code {
    private MyDataViewer _viewer = null!;
}
```

## Real-World Example: Moka.Blazor.Json.AI

The [Moka.Blazor.Json.AI](https://github.com/jacobwi/Moka.Blazor.Json) package follows this exact pattern:

| Concept | Implementation |
|---------|---------------|
| Context builder | `JsonContextBuilder` — extracts JSON from the viewer, truncates large docs, includes selected subtree |
| Wrapper component | `MokaJsonAiPanel` — composes `MokaAiPanel`, adds "Selection" button and JSON-specific quick actions |
| System prompt | Tailored for JSON analysis (paths, schemas, transformations) |
| Quick actions | Summarize, Analyze, Schema — with JSON-specific prompts |
| DI registration | `AddMokaJsonAi()` calls `AddMokaAi()` then registers `JsonContextBuilder` as scoped `IAiContextBuilder` |
| Public API | `AskAboutNode(path)` and `CreateAskAiContextAction()` for context menu integration |

## Scoping Context

By default, `BuildContext()` returns the entire data source. When users ask about a specific part — a JSON node, a code function, a database row — you can **scope** the context so the AI only sees the relevant data.

### The Scope API

`IAiContextBuilder` provides three default methods for scoping:

```csharp
// Set a named scope — narrows what BuildContext() returns
builder.SetScope("path", "/users/0");

// Clear a specific scope
builder.ClearScope("path");

// Clear all scopes
builder.ClearScope();

// Read active scopes
IReadOnlyDictionary<string, object?> scopes = builder.GetScopes();
```

### Implementing Scope in Your Builder

Override the default no-op methods and check scopes inside `BuildContext()`:

```csharp
internal sealed class MyContextBuilder : IAiContextBuilder
{
    private readonly Dictionary<string, object?> _scopes = new(StringComparer.OrdinalIgnoreCase);
    private MyDataSource? _dataSource;

    public string BuildContext(AiChatOptions options)
    {
        if (_dataSource is null)
            return "[No data source connected]";

        // Check if we're scoped to a specific item
        if (_scopes.TryGetValue("item", out var scopeData) && scopeData is string itemId)
        {
            string item = _dataSource.GetItem(itemId);
            if (!string.IsNullOrEmpty(item))
                return $"[Scoped to: {itemId}]\n{item}";
        }

        // Default: return full data
        return _dataSource.GetCurrentData();
    }

    public void SetScope(string key, object? data) => _scopes[key] = data;

    public void ClearScope(string? key = null)
    {
        if (key is null)
            _scopes.Clear();
        else
            _scopes.Remove(key);
    }

    public IReadOnlyDictionary<string, object?> GetScopes() => _scopes;

    internal void SetDataSource(MyDataSource? source) => _dataSource = source;
}
```

### Using Scopes in Your Wrapper Component

Set scope before sending a message, then clear it afterward so general questions return to full context:

```csharp
public async Task AskAboutItem(string itemId)
{
    if (_panel is null || _panel.IsSending)
        return;

    // Scope context to this item
    ContextBuilder.SetScope("item", itemId);

    await _panel.SendToAi($"Analyze item `{itemId}`.");

    // Clear scope so the next general question sees everything
    ContextBuilder.ClearScope("item");
}

// For persistent scoping (user explicitly narrows focus):
public void FocusOn(string itemId) => ContextBuilder.SetScope("item", itemId);
public void Unfocus() => ContextBuilder.ClearScope();
```

### How Moka.Blazor.Json.AI Uses Scoping

The JSON AI panel uses the `"path"` scope to extract subtrees:

- **Right-click → Ask AI**: sets `scope("path", "/users/0")`, sends prompt, clears scope after
- **`ScopeToNode(path)`**: persistent scope — all subsequent questions focus on that subtree
- **`ClearScope()`**: returns to full-document context

This means a 50MB JSON document can be analyzed node-by-node without exceeding the AI context window.

## Tips

- **Composition over inheritance** — hold a `@ref` to `MokaAiPanel` rather than subclassing it. This keeps the API surface clean and avoids cross-assembly access issues.
- **Scoped DI** — always register stateful services as Scoped in Blazor Server. Transient creates a new instance per injection, breaking the `SetX()` pattern.
- **`ActionsExtra`** — use this `RenderFragment` to inject custom buttons into the panel's action bar without forking the base component.
- **`SendToAi()` is public** — call it from your wrapper to send programmatic prompts (e.g., from a context menu action or button click).
- **System prompt** — tailor it to your domain. Tell the AI what data format to expect, how to reference paths/fields, and what analysis patterns to follow.
