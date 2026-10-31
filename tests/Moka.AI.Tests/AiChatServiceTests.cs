using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Moka.AI.Models;
using Moka.AI.Services;
using Xunit;

namespace Moka.AI.Tests;

public sealed class AiChatServiceTests
{
	private static AiChatService CreateService(
		FakeChatClient client,
		AiChatOptions? options = null,
		IAiContextBuilder? contextBuilder = null)
		=> new(client, Options.Create(options ?? new AiChatOptions()), contextBuilder);

	[Fact]
	public async Task SendAsync_BuildsExpectedMessageSequence()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client);
		var history = new List<AiMessage>
		{
			new() { Role = AiMessageRole.User, Content = "earlier question" },
			new() { Role = AiMessageRole.Assistant, Content = "earlier answer" }
		};

		string response = await service.SendAsync("current question", "You are a test.", history);

		Assert.Equal("ok", response);
		List<ChatMessage> sent = Assert.Single(client.Requests);
		Assert.Equal(4, sent.Count);
		Assert.Equal(ChatRole.System, sent[0].Role);
		Assert.Equal("You are a test.", sent[0].Text);
		Assert.Equal(ChatRole.User, sent[1].Role);
		Assert.Equal("earlier question", sent[1].Text);
		Assert.Equal(ChatRole.Assistant, sent[2].Role);
		Assert.Equal("earlier answer", sent[2].Text);
		Assert.Equal(ChatRole.User, sent[3].Role);
		Assert.Equal("current question", sent[3].Text);
	}

	[Fact]
	public async Task SendAsync_SkipsSystemMessagesInHistory()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client);
		var history = new List<AiMessage>
		{
			new() { Role = AiMessageRole.System, Content = "leaked system note" },
			new() { Role = AiMessageRole.User, Content = "q1" }
		};

		await service.SendAsync("q2", "sys prompt", history);

		List<ChatMessage> sent = Assert.Single(client.Requests);
		Assert.Equal(3, sent.Count);
		Assert.DoesNotContain(sent, m => m.Text.Contains("leaked system note"));
	}

	[Fact]
	public async Task SendAsync_AppendsContextToSystemPrompt()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client, contextBuilder: new StubContextBuilder("DB has 42 rows."));

		await service.SendAsync("q", "base prompt", []);

		List<ChatMessage> sent = Assert.Single(client.Requests);
		Assert.Equal(ChatRole.System, sent[0].Role);
		Assert.Contains("base prompt", sent[0].Text);
		Assert.Contains("Context:", sent[0].Text);
		Assert.Contains("DB has 42 rows.", sent[0].Text);
	}

	[Fact]
	public async Task SendAsync_SendsModelIdAndTemperature()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client, new AiChatOptions { DefaultModel = "test-model", Temperature = 0.7f });

		await service.SendAsync("q", "sys", []);

		ChatOptions? options = Assert.Single(client.OptionsRequests);
		Assert.NotNull(options);
		Assert.Equal("test-model", options.ModelId);
		Assert.Equal(0.7f, options.Temperature);
	}

	[Fact]
	public async Task SetModel_TakesEffectOnNextRequest()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client);

		await service.SendAsync("q", "sys", []);
		service.SetModel("new-model");
		await service.SendAsync("q", "sys", []);

		ChatOptions? second = client.OptionsRequests[1];
		Assert.NotNull(second);
		Assert.Equal("new-model", second.ModelId);
	}

	[Fact]
	public async Task StreamAsync_YieldsFullResponseText()
	{
		using var client = new FakeChatClient { ResponseText = "hello world" };
		var service = CreateService(client);

		var sb = new System.Text.StringBuilder();
		await foreach (string token in service.StreamAsync("hi", "sys", []))
		{
			sb.Append(token);
		}

		Assert.Equal("hello world", sb.ToString());
	}

	[Fact]
	public void Options_AreIsolatedPerServiceInstance()
	{
		using var client = new FakeChatClient();
		var source = Options.Create(new AiChatOptions());
		var serviceA = new AiChatService(client, source);
		var serviceB = new AiChatService(client, source);

		serviceA.SetTemperature(1.5f);
		serviceA.SetModel("model-a");
		serviceA.SetMaxContextChars(1234);
		serviceA.SetStreamResponses(false);

		Assert.Equal(1.5f, serviceA.Options.Temperature);
		Assert.Equal("model-a", serviceA.Options.DefaultModel);
		Assert.Equal(1234, serviceA.Options.MaxContextChars);
		Assert.False(serviceA.Options.StreamResponses);

		// The other instance sees the configured defaults, never A's changes
		Assert.Equal(0.3f, serviceB.Options.Temperature);
		Assert.Equal("local-model", serviceB.Options.DefaultModel);
		Assert.Equal(8000, serviceB.Options.MaxContextChars);
		Assert.True(serviceB.Options.StreamResponses);

		// The configured options singleton itself is never mutated
		Assert.Equal(0.3f, source.Value.Temperature);
		Assert.Equal("local-model", source.Value.DefaultModel);
	}

	[Fact]
	public async Task TestConnectionAsync_ReturnsTrueAndModelName_WhenBackendResponds()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client, new AiChatOptions { DefaultModel = "m1" });

		(bool connected, string modelName) = await service.TestConnectionAsync();

		Assert.True(connected);
		Assert.Equal("m1", modelName);
	}

	[Fact]
	public async Task TestConnectionAsync_ReturnsFalse_WhenBackendThrows()
	{
		using var client = new FakeChatClient { ThrowOnRequest = true };
		var service = CreateService(client);

		(bool connected, _) = await service.TestConnectionAsync();

		Assert.False(connected);
	}

	[Fact]
	public void BuildContext_ReturnsEmptyString_WhenNoBuilderRegistered()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client);

		Assert.Equal("", service.BuildContext());
	}

	[Fact]
	public void BuildContext_ReturnsBuilderOutput()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client, contextBuilder: new StubContextBuilder("ctx-data"));

		Assert.Equal("ctx-data", service.BuildContext());
	}

	private sealed class StubContextBuilder(string context) : IAiContextBuilder
	{
		public string BuildContext(AiChatOptions options) => context;
	}
}
