using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Moka.AI.Models;

namespace Moka.AI.Services;

/// <summary>
///     Generic AI chat service that wraps an <see cref="IChatClient" /> for streaming
///     and non-streaming chat interactions. Domain-specific context is provided via
///     <see cref="IAiContextBuilder" />.
/// </summary>
public sealed class AiChatService(
	IChatClient chatClient,
	IOptions<AiChatOptions> options,
	IAiContextBuilder? contextBuilder = null)
{
	/// <summary>
	///     The current AI options. This is a per-instance snapshot of the configured
	///     <see cref="AiChatOptions" /> — runtime changes made via the <c>Set*</c> methods
	///     affect only this service instance (and therefore only the current scope),
	///     never other scopes or users.
	/// </summary>
	public AiChatOptions Options { get; } = Snapshot(options.Value);

	/// <summary>
	///     Change the model at runtime. Takes effect on the next request — the model id
	///     is sent with every request, so this works even for clients whose chat client
	///     was created with a different default model.
	/// </summary>
	public void SetModel(string modelName) => Options.DefaultModel = modelName;

	/// <summary>
	///     Change the temperature at runtime.
	/// </summary>
	public void SetTemperature(float temperature) => Options.Temperature = temperature;

	/// <summary>
	///     Change the max context chars at runtime.
	/// </summary>
	public void SetMaxContextChars(int chars) => Options.MaxContextChars = chars;

	/// <summary>
	///     Change the stream responses setting at runtime.
	/// </summary>
	public void SetStreamResponses(bool stream) => Options.StreamResponses = stream;

	/// <summary>
	///     Sends a message and returns the full response.
	/// </summary>
	public async Task<string> SendAsync(
		string userMessage,
		string systemPrompt,
		List<AiMessage> history,
		CancellationToken cancellationToken = default)
	{
		List<ChatMessage> messages = BuildMessages(userMessage, systemPrompt, history);

		var options = new ChatOptions
		{
			Temperature = Options.Temperature,
			ModelId = Options.DefaultModel
		};

		ChatResponse response = await chatClient.GetResponseAsync(
			messages,
			options,
			cancellationToken);

		return response.Text ?? "";
	}

	/// <summary>
	///     Sends a message and streams the response token-by-token.
	/// </summary>
	public async IAsyncEnumerable<string> StreamAsync(
		string userMessage,
		string systemPrompt,
		List<AiMessage> history,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		List<ChatMessage> messages = BuildMessages(userMessage, systemPrompt, history);

		var options = new ChatOptions
		{
			Temperature = Options.Temperature,
			ModelId = Options.DefaultModel
		};

		await foreach (ChatResponseUpdate update in chatClient.GetStreamingResponseAsync(
			               messages, options, cancellationToken))
		{
			if (update.Text is not null)
			{
				yield return update.Text;
			}
		}
	}

	/// <summary>
	///     Tests connectivity to the AI backend.
	/// </summary>
	public async Task<(bool Connected, string ModelName)> TestConnectionAsync(
		CancellationToken cancellationToken = default)
	{
		try
		{
			await chatClient.GetResponseAsync(
				[new ChatMessage(ChatRole.User, "Say 'ok'")],
				new ChatOptions { Temperature = 0, MaxOutputTokens = 5 },
				cancellationToken);

			return (true, Options.DefaultModel ?? "local-model");
		}
		catch
		{
			return (false, Options.DefaultModel ?? "local-model");
		}
	}

	/// <summary>
	///     Builds the context string from the registered <see cref="IAiContextBuilder" />.
	///     Returns empty string if no context builder is registered.
	/// </summary>
	public string BuildContext() => contextBuilder?.BuildContext(Options) ?? "";

	private static AiChatOptions Snapshot(AiChatOptions source) => new()
	{
		Provider = source.Provider,
		Endpoint = source.Endpoint,
		ApiKey = source.ApiKey,
		DefaultModel = source.DefaultModel,
		SystemPrompt = source.SystemPrompt,
		MaxContextChars = source.MaxContextChars,
		MaxHistoryChars = source.MaxHistoryChars,
		StreamResponses = source.StreamResponses,
		Temperature = source.Temperature
	};

	private List<ChatMessage> BuildMessages(
		string userMessage,
		string systemPrompt,
		List<AiMessage> history)
	{
		// Build system message with context
		string context = BuildContext();
		string fullSystemPrompt = string.IsNullOrEmpty(context)
			? systemPrompt
			: $"{systemPrompt}\n\n---\nContext:\n{context}";

		// Trim history to the configured budget (system entries never take part)
		List<AiMessage> conversation = history.Where(m => m.Role != AiMessageRole.System).ToList();
		bool truncated;
		List<AiMessage> selected = TrimHistory(conversation, out truncated);

		if (truncated)
		{
			fullSystemPrompt += "\n\n[Note: earlier conversation history was truncated to fit the context window.]";
		}

		var messages = new List<ChatMessage>
		{
			new(ChatRole.System, fullSystemPrompt)
		};

		foreach (AiMessage msg in selected)
		{
			ChatRole role = msg.Role == AiMessageRole.User ? ChatRole.User : ChatRole.Assistant;
			messages.Add(new ChatMessage(role, msg.Content));
		}

		messages.Add(new ChatMessage(ChatRole.User, userMessage));

		return messages;
	}

	/// <summary>
	///     Drops the oldest history messages until the total content fits within
	///     <see cref="AiChatOptions.MaxHistoryChars" />. Always keeps at least one
	///     message, and re-aligns so the kept history starts with a user message.
	/// </summary>
	private List<AiMessage> TrimHistory(List<AiMessage> conversation, out bool truncated)
	{
		truncated = false;

		int budget = Options.MaxHistoryChars;
		if (budget <= 0 || conversation.Count <= 1)
		{
			return conversation;
		}

		int total = 0;
		foreach (AiMessage msg in conversation)
		{
			total += msg.Content.Length;
		}

		if (total <= budget)
		{
			return conversation;
		}

		truncated = true;

		int start = 0;
		while (total > budget && start < conversation.Count - 1)
		{
			total -= conversation[start].Content.Length;
			start++;
		}

		List<AiMessage> selected = conversation[start..];

		// Some providers expect the history after the system message to open with
		// a user turn — drop a leading assistant message left behind by trimming.
		if (selected.Count > 1 && selected[0].Role == AiMessageRole.Assistant)
		{
			selected = selected[1..];
		}

		return selected;
	}
}
