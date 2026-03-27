using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Moka.Blazor.AI.Models;

namespace Moka.Blazor.AI.Services;

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
	///     The current AI options configuration.
	/// </summary>
	public AiChatOptions Options { get; } = options.Value;

	/// <summary>
	///     Change the model at runtime. Takes effect on the next request.
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

		var options = new ChatOptions { Temperature = Options.Temperature };

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

		var options = new ChatOptions { Temperature = Options.Temperature };

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

		var messages = new List<ChatMessage>
		{
			new(ChatRole.System, fullSystemPrompt)
		};

		// Add conversation history (skip system messages)
		foreach (AiMessage msg in history)
		{
			if (msg.Role == AiMessageRole.System)
			{
				continue;
			}

			ChatRole role = msg.Role == AiMessageRole.User ? ChatRole.User : ChatRole.Assistant;
			messages.Add(new ChatMessage(role, msg.Content));
		}

		messages.Add(new ChatMessage(ChatRole.User, userMessage));

		return messages;
	}
}
