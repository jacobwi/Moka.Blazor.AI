namespace Moka.Blazor.AI.Models;

/// <summary>
///     A single message in an AI conversation.
/// </summary>
public sealed class AiMessage
{
	/// <summary>Role of the message author.</summary>
	public required AiMessageRole Role { get; init; }

	/// <summary>Message content (may be streamed incrementally).</summary>
	public string Content { get; set; } = "";

	/// <summary>When the message was created.</summary>
	public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

	/// <summary>Whether this message is still being streamed.</summary>
	public bool IsStreaming { get; set; }

	/// <summary>Whether the user cancelled/stopped this response mid-stream.</summary>
	public bool IsCancelled { get; set; }

	/// <summary>Duration of the streaming response in milliseconds.</summary>
	public long DurationMs { get; set; }

	/// <summary>Estimated token count (approximate: chars / 4).</summary>
	public int EstimatedTokens { get; set; }
}

/// <summary>
///     Role of a message in the conversation.
/// </summary>
public enum AiMessageRole
{
	/// <summary>User-sent message.</summary>
	User,

	/// <summary>AI assistant response.</summary>
	Assistant,

	/// <summary>System/error message.</summary>
	System
}
