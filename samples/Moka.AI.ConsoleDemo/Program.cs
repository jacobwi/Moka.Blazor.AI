// Demonstrates using Moka.AI from a plain console app — no Blazor, no ASP.NET,
// just Microsoft.Extensions.DependencyInjection and an IChatClient provider.

using Microsoft.Extensions.DependencyInjection;
using Moka.AI.Extensions;
using Moka.AI.Models;
using Moka.AI.Services;

var services = new ServiceCollection();

services.AddMokaAi(options =>
{
	options.Provider = AiProvider.OpenAiCompatible;
	options.Endpoint = "http://localhost:1234/v1"; // LM Studio default
	options.DefaultModel = "qwen2.5-3b";
});
services.AddScoped<IAiContextBuilder, AppContextBuilder>();

await using var provider = services.BuildServiceProvider();

// ── 1. Runtime settings are isolated per scope ──
// Each AiChatService snapshots its options, so changes made in one scope
// (here: one simulated user) never leak into another.
await using (var scopeA = provider.CreateAsyncScope())
await using (var scopeB = provider.CreateAsyncScope())
{
	var chatA = scopeA.ServiceProvider.GetRequiredService<AiChatService>();
	var chatB = scopeB.ServiceProvider.GetRequiredService<AiChatService>();

	chatA.SetTemperature(1.5f);

	Console.WriteLine($"Scope A temperature: {chatA.Options.Temperature}"); // 1.5
	Console.WriteLine($"Scope B temperature: {chatB.Options.Temperature}"); // 0.3 — unaffected
}

// ── 2. Domain context via IAiContextBuilder ──
await using (var scope = provider.CreateAsyncScope())
{
	var chat = scope.ServiceProvider.GetRequiredService<AiChatService>();
	Console.WriteLine($"Injected context: {chat.BuildContext()}");
}

// ── 3. Live streaming round-trip (skipped when no local server is running) ──
await using (var scope = provider.CreateAsyncScope())
{
	var chat = scope.ServiceProvider.GetRequiredService<AiChatService>();
	(bool connected, string model) = await chat.TestConnectionAsync();

	if (!connected)
	{
		Console.WriteLine($"No AI server reachable at {chat.Options.ResolvedEndpoint} — skipping live chat demo.");
		return;
	}

	Console.WriteLine($"Connected to {model}. Streaming response:");
	Console.WriteLine();

	var history = new List<AiMessage>();
	await foreach (string token in chat.StreamAsync(
		               "Say hello in one short sentence.",
		               "You are a terse assistant.",
		               history))
	{
		Console.Write(token);
	}

	Console.WriteLine();
}

/// <summary>Example domain-specific context injected into every prompt.</summary>
internal sealed class AppContextBuilder : IAiContextBuilder
{
	public string BuildContext(AiChatOptions options) =>
		$"The application has 3 open documents and 42 pending notifications.";
}
