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
		serviceA.Options.MaxHistoryChars = 500;

		Assert.Equal(1.5f, serviceA.Options.Temperature);
		Assert.Equal("model-a", serviceA.Options.DefaultModel);
		Assert.Equal(1234, serviceA.Options.MaxContextChars);
		Assert.Equal(500, serviceA.Options.MaxHistoryChars);
		Assert.False(serviceA.Options.StreamResponses);

		// The other instance sees the configured defaults, never A's changes
		Assert.Equal(0.3f, serviceB.Options.Temperature);
		Assert.Equal("local-model", serviceB.Options.DefaultModel);
		Assert.Equal(8000, serviceB.Options.MaxContextChars);
		Assert.Equal(12000, serviceB.Options.MaxHistoryChars);
		Assert.True(serviceB.Options.StreamResponses);

		// The configured options singleton itself is never mutated
		Assert.Equal(0.3f, source.Value.Temperature);
		Assert.Equal("local-model", source.Value.DefaultModel);
		Assert.Equal(12000, source.Value.MaxHistoryChars);
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

	[Fact]
	public async Task SendAsync_TrimsOldestHistory_WhenOverBudget()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client, new AiChatOptions { MaxHistoryChars = 10 });
		var history = new List<AiMessage>
		{
			new() { Role = AiMessageRole.User, Content = "0123456789" }, // 10 chars — alone fills the budget
			new() { Role = AiMessageRole.User, Content = "b" },
			new() { Role = AiMessageRole.Assistant, Content = "a" }
		};

		await service.SendAsync("q", "sys", history);

		List<ChatMessage> sent = Assert.Single(client.Requests);
		Assert.Equal(4, sent.Count); // system + 2 kept history + current
		Assert.Equal("b", sent[1].Text);
		Assert.Equal("a", sent[2].Text);
		Assert.Contains("truncated", sent[0].Text);
	}

	[Fact]
	public async Task SendAsync_KeepsFullHistory_WhenUnderBudget()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client, new AiChatOptions { MaxHistoryChars = 1000 });
		var history = new List<AiMessage>
		{
			new() { Role = AiMessageRole.User, Content = "q1" },
			new() { Role = AiMessageRole.Assistant, Content = "a1" }
		};

		await service.SendAsync("q2", "sys", history);

		List<ChatMessage> sent = Assert.Single(client.Requests);
		Assert.Equal(4, sent.Count);
		Assert.DoesNotContain("truncated", sent[0].Text);
	}

	[Fact]
	public async Task SendAsync_TrimsToUserFirstAlignment()
	{
		using var client = new FakeChatClient();
		// Budget 6: drops the first user msg (total 8 -> 6), leaving an assistant
		// message first — the aligner must skip it so history opens with a user turn.
		var service = CreateService(client, new AiChatOptions { MaxHistoryChars = 6 });
		var history = new List<AiMessage>
		{
			new() { Role = AiMessageRole.User, Content = "u1" },
			new() { Role = AiMessageRole.Assistant, Content = "a1" },
			new() { Role = AiMessageRole.User, Content = "u2" },
			new() { Role = AiMessageRole.Assistant, Content = "a2" }
		};

		await service.SendAsync("q", "sys", history);

		List<ChatMessage> sent = Assert.Single(client.Requests);
		// system + [u2, a2] + current — the leading a1 was aligned away
		Assert.Equal(4, sent.Count);
		Assert.Equal(ChatRole.User, sent[1].Role);
		Assert.Equal("u2", sent[1].Text);
		Assert.Equal("a2", sent[2].Text);
	}

	[Fact]
	public async Task SendAsync_KeepsSingleOversizedHistoryMessage()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client, new AiChatOptions { MaxHistoryChars = 5 });
		var history = new List<AiMessage>
		{
			new() { Role = AiMessageRole.User, Content = "0123456789" }
		};

		await service.SendAsync("q", "sys", history);

		// Never drops the last remaining history message, even when it alone exceeds the budget
		List<ChatMessage> sent = Assert.Single(client.Requests);
		Assert.Equal(3, sent.Count);
		Assert.Equal("0123456789", sent[1].Text);
	}

	[Fact]
	public async Task SendAsync_DisabledBudget_KeepsAllHistory()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client, new AiChatOptions { MaxHistoryChars = 0 });
		var history = new List<AiMessage>
		{
			new() { Role = AiMessageRole.User, Content = new string('x', 5000) },
			new() { Role = AiMessageRole.Assistant, Content = new string('y', 5000) },
			new() { Role = AiMessageRole.User, Content = new string('z', 5000) }
		};

		await service.SendAsync("q", "sys", history);

		List<ChatMessage> sent = Assert.Single(client.Requests);
		Assert.Equal(5, sent.Count); // system + all 3 history + current
		Assert.DoesNotContain("truncated", sent[0].Text);
	}

	// ── Tool calling ──

	[Fact]
	public async Task SendAsync_PassesRegisteredTools()
	{
		using var client = new FakeChatClient();
		AIFunction tool = AIFunctionFactory.Create((int a, int b) => a + b,
			new AIFunctionFactoryOptions { Name = "add" });
		var service = CreateService(client, new AiChatOptions());
		service.Options.Tools.Add(tool);

		await service.SendAsync("q", "sys", []);

		ChatOptions? options = Assert.Single(client.OptionsRequests);
		Assert.NotNull(options);
		Assert.NotNull(options.Tools);
		Assert.Contains(tool, options.Tools);
	}

	[Fact]
	public async Task SendAsync_NoToolsRegistered_SendsEmptyToolList()
	{
		using var client = new FakeChatClient();
		var service = CreateService(client);

		await service.SendAsync("q", "sys", []);

		ChatOptions? options = Assert.Single(client.OptionsRequests);
		Assert.NotNull(options);
		Assert.True(options.Tools is null or { Count: 0 });
	}

	[Fact]
	public async Task SendAsync_ExecutesToolCallLoop_EndToEnd()
	{
		using var client = new FakeChatClient();

		int? toolResult = null;
		AIFunction addTool = AIFunctionFactory.Create(
			(int a, int b) => toolResult = a + b,
			new AIFunctionFactoryOptions { Name = "add", Description = "Add two integers." });

		// Script: first the model asks to call add(2, 3); after the result round-trips
		// it answers with the sum.
		client.ScriptedResponses = new Queue<ChatResponse>(
		[
			new ChatResponse(new ChatMessage(ChatRole.Assistant,
				[new FunctionCallContent("call-1", "add",
					new Dictionary<string, object?> { ["a"] = 2, ["b"] = 3 })])),
			new ChatResponse(new ChatMessage(ChatRole.Assistant, "The sum is 5."))
		]);

		var service = CreateService(client, new AiChatOptions());
		service.Options.Tools.Add(addTool);

		string response = await service.SendAsync("What is 2 + 3?", "sys", []);

		Assert.Equal(2, client.Requests.Count);

		// The second request must carry the tool result back to the model
		List<ChatMessage> second = client.Requests[1];
		FunctionResultContent resultContent = second
			.SelectMany(m => m.Contents)
			.OfType<FunctionResultContent>()
			.Single();
		Assert.True(resultContent.Exception is null, resultContent.Exception?.ToString());
		Assert.Equal("The sum is 5.", response);
		Assert.Equal(5, toolResult);
	}

	[Fact]
	public void Tools_AreSharedBetweenServiceInstances()
	{
		using var client = new FakeChatClient();
		var source = Options.Create(new AiChatOptions());
		var serviceA = new AiChatService(client, source);
		var serviceB = new AiChatService(client, source);

		AIFunction tool = AIFunctionFactory.Create((int a, int b) => a + b,
			new AIFunctionFactoryOptions { Name = "add" });
		serviceA.Options.Tools.Add(tool);

		// App-level state: B sees tools registered through A after construction
		Assert.Contains(tool, serviceB.Options.Tools);
	}

	private sealed class StubContextBuilder(string context) : IAiContextBuilder
	{
		public string BuildContext(AiChatOptions options) => context;
	}
}
