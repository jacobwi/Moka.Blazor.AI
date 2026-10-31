using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Moka.AI.Tests;

/// <summary>
///     In-memory <see cref="IChatClient" /> that records every request and returns
///     canned responses — no network required.
/// </summary>
internal sealed class FakeChatClient : IChatClient
{
	public List<List<ChatMessage>> Requests { get; } = [];

	public List<ChatOptions?> OptionsRequests { get; } = [];

	public string ResponseText { get; set; } = "ok";

	public bool ThrowOnRequest { get; set; }

	public Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		Record(messages, options);

		if (ThrowOnRequest)
		{
			throw new InvalidOperationException("simulated backend failure");
		}

		return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, ResponseText)));
	}

	public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		Record(messages, options);

		foreach (char c in ResponseText)
		{
			await Task.Yield();
			yield return new ChatResponseUpdate(ChatRole.Assistant, c.ToString());
		}
	}

	public object? GetService(Type serviceType, object? serviceKey = null) => null;

	public void Dispose()
	{
	}

	private void Record(IEnumerable<ChatMessage> messages, ChatOptions? options)
	{
		Requests.Add([.. messages]);
		OptionsRequests.Add(options);
	}
}
