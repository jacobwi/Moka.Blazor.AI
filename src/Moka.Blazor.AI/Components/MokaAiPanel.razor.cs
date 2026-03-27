using System.Diagnostics;
using System.Text;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Moka.Blazor.AI.Models;
using Moka.Blazor.AI.Services;

namespace Moka.Blazor.AI.Components;

/// <summary>
///     A reusable AI chat panel component with streaming, settings, markdown rendering,
///     copy, stop, edit/re-send, and configurable quick actions.
/// </summary>
public partial class MokaAiPanel : ComponentBase, IDisposable
{
	private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
		.UseAdvancedExtensions()
		.Build();

	private readonly List<AiMessage> _messages = [];
	private bool _connectionChecked;
	private string? _copiedMessageId;
	private int _editingMessageIndex = -1;
	private string _editText = "";

	private string _inputText = "";
	private bool _isCollapsed;
	private int _lastContextChars;
	private ElementReference _messagesRef;
	private CancellationTokenSource? _sendCts;
	private bool _showSettings;
	private string _statusText = "Connecting...";
	private int _totalEstimatedTokens;

	[Inject] private AiChatService ChatService { get; set; } = null!;
	[Inject] private IJSRuntime JS { get; set; } = null!;

	/// <summary>
	///     System prompt for the AI. Consumers provide domain-specific instructions.
	/// </summary>
	[Parameter]
	public string SystemPrompt { get; set; } = "You are a helpful AI assistant.";

	/// <summary>
	///     Configurable quick action buttons. Each has a label and prompt.
	/// </summary>
	[Parameter]
	public IReadOnlyList<AiQuickAction>? QuickActions { get; set; }

	/// <summary>
	///     Optional extra content rendered in the quick actions bar (e.g., domain-specific buttons).
	/// </summary>
	[Parameter]
	public RenderFragment? ActionsExtra { get; set; }

	/// <summary>
	///     Title text shown in the panel header. Default is <c>"AI Assistant"</c>.
	/// </summary>
	[Parameter]
	public string Title { get; set; } = "AI Assistant";

	/// <summary>
	///     Maximum height of the messages area. Default is <c>"350px"</c>.
	/// </summary>
	[Parameter]
	public string MessagesHeight { get; set; } = "350px";

	/// <summary>
	///     Placeholder text for the input field.
	/// </summary>
	[Parameter]
	public string Placeholder { get; set; } = "Ask a question...";

	/// <summary>
	///     Whether to show the quick action buttons. Default is <c>true</c>.
	/// </summary>
	[Parameter]
	public bool ShowQuickActions { get; set; } = true;

	/// <summary>
	///     CSS theme attribute value (e.g., "light", "dark", "auto").
	/// </summary>
	[Parameter]
	public string ThemeAttribute { get; set; } = "";

	/// <summary>
	///     Visual style for chat messages. Default is <see cref="ChatStyle.Bubble" />.
	/// </summary>
	[Parameter]
	public ChatStyle ChatStyle { get; set; } = ChatStyle.Bubble;

	/// <summary>
	///     The current AI chat service for subclasses to access.
	/// </summary>
	protected AiChatService Service => ChatService;

	/// <summary>
	///     The current connection status.
	/// </summary>
	protected string ConnectionStatus { get; private set; } = "unknown";

	/// <summary>
	///     Whether a message is currently being sent/streamed.
	/// </summary>
	public bool IsSending { get; private set; }

	/// <summary>
	///     The conversation messages.
	/// </summary>
	protected IReadOnlyList<AiMessage> Messages => _messages;

	private string CurrentModel => ChatService.Options.DefaultModel ?? "local-model";
	private float CurrentTemperature => ChatService.Options.Temperature;
	private int CurrentMaxContext => ChatService.Options.MaxContextChars;
	private bool CurrentStreamResponses => ChatService.Options.StreamResponses;
	private ChatStyle CurrentChatStyle => ChatStyle;

	private string ChatStyleCssClass => ChatStyle switch
	{
		ChatStyle.Classic => "style-classic",
		ChatStyle.Compact => "style-compact",
		_ => "style-bubble"
	};

	public void Dispose()
	{
		_sendCts?.Cancel();
		_sendCts?.Dispose();
	}

	protected override async Task OnAfterRenderAsync(bool firstRender)
	{
		if (!_connectionChecked)
		{
			_connectionChecked = true;
			await CheckConnectionAsync();
		}

		if (_messages.Count > 0)
		{
			await ScrollToBottomAsync();
		}
	}

	private async Task CheckConnectionAsync()
	{
		ConnectionStatus = "unknown";
		_statusText = "Connecting...";
		StateHasChanged();

		try
		{
			(bool connected, string modelName) = await ChatService.TestConnectionAsync();
			if (connected)
			{
				ConnectionStatus = "connected";
				_statusText = modelName;
			}
			else
			{
				ConnectionStatus = "disconnected";
				_statusText = "Not connected";
			}
		}
		catch
		{
			ConnectionStatus = "disconnected";
			_statusText = "Not connected";
		}

		StateHasChanged();
	}

	private async Task RetryConnection()
	{
		_connectionChecked = false;
		await CheckConnectionAsync();
	}

	private async Task SendMessage()
	{
		if (string.IsNullOrWhiteSpace(_inputText) || IsSending)
		{
			return;
		}

		string userText = _inputText.Trim();
		_inputText = "";

		await SendToAi(userText);
	}

	/// <summary>
	///     Sends a quick action prompt to the AI.
	/// </summary>
	protected async Task SendQuickAction(AiQuickAction action)
	{
		if (IsSending)
		{
			return;
		}

		await SendToAi(action.Prompt);
	}

	/// <summary>
	///     Sends a message to the AI. Can be called by composing or subclass components.
	/// </summary>
	public async Task SendToAi(string userText)
	{
		_messages.Add(new AiMessage { Role = AiMessageRole.User, Content = userText });

		var assistantMessage = new AiMessage
		{
			Role = AiMessageRole.Assistant,
			Content = "",
			IsStreaming = true
		};
		_messages.Add(assistantMessage);

		IsSending = true;
		_lastContextChars = 0;
		StateHasChanged();

		_sendCts?.Dispose();
		_sendCts = new CancellationTokenSource();

		var stopwatch = Stopwatch.StartNew();

		try
		{
			string context = ChatService.BuildContext();
			_lastContextChars = context.Length;

			var sb = new StringBuilder();
			int tokenCount = 0;
			await foreach (string token in ChatService.StreamAsync(
				               userText, SystemPrompt, _messages[..^2], _sendCts.Token))
			{
				sb.Append(token);
				assistantMessage.Content = sb.ToString();
				tokenCount++;

				if (tokenCount % 3 == 0 || sb.Length < 50)
				{
					StateHasChanged();
					await ScrollToBottomAsync();
				}
			}

			stopwatch.Stop();
			assistantMessage.IsStreaming = false;
			assistantMessage.DurationMs = stopwatch.ElapsedMilliseconds;
			assistantMessage.EstimatedTokens = Math.Max(1, assistantMessage.Content.Length / 4);
			RecalculateTotalTokens();
		}
		catch (OperationCanceledException)
		{
			stopwatch.Stop();
			assistantMessage.IsCancelled = true;
			assistantMessage.IsStreaming = false;
			assistantMessage.DurationMs = stopwatch.ElapsedMilliseconds;
			assistantMessage.EstimatedTokens = Math.Max(1, assistantMessage.Content.Length / 4);
			RecalculateTotalTokens();
		}
		catch (Exception ex)
		{
			stopwatch.Stop();
			assistantMessage.IsStreaming = false;
			assistantMessage.Content = "";

			_messages.Add(new AiMessage
			{
				Role = AiMessageRole.System,
				Content = ex.Message
			});
		}
		finally
		{
			IsSending = false;
			StateHasChanged();
		}
	}

	// ── Edit message ──

	private void StartEditMessage(int index)
	{
		if (index < 0 || index >= _messages.Count || _messages[index].Role != AiMessageRole.User)
		{
			return;
		}

		_editingMessageIndex = index;
		_editText = _messages[index].Content;
		StateHasChanged();
	}

	private void CancelEdit()
	{
		_editingMessageIndex = -1;
		_editText = "";
		StateHasChanged();
	}

	private async Task SubmitEdit()
	{
		if (string.IsNullOrWhiteSpace(_editText) || _editingMessageIndex < 0)
		{
			return;
		}

		int editIndex = _editingMessageIndex;
		string newText = _editText.Trim();

		_editingMessageIndex = -1;
		_editText = "";

		// Remove all messages from the edited message onward
		if (editIndex < _messages.Count)
		{
			_messages.RemoveRange(editIndex, _messages.Count - editIndex);
		}

		RecalculateTotalTokens();

		// Re-send with the edited text
		await SendToAi(newText);
	}

	private async Task HandleEditKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter" && !e.ShiftKey)
		{
			await SubmitEdit();
		}
		else if (e.Key == "Escape")
		{
			CancelEdit();
		}
	}

	private void RecalculateTotalTokens()
	{
		_totalEstimatedTokens = 0;
		foreach (AiMessage msg in _messages)
		{
			if (msg.Role == AiMessageRole.Assistant)
			{
				_totalEstimatedTokens += msg.EstimatedTokens;
			}
		}
	}

	// ── Core actions ──

	private void StopStreaming() => _sendCts?.Cancel();

	private void ToggleCollapse() => _isCollapsed = !_isCollapsed;

	private void ToggleSettings()
	{
		_showSettings = !_showSettings;
		StateHasChanged();
	}

	private void DismissSettings()
	{
		_showSettings = false;
		StateHasChanged();
	}

	private void HandleModelChange(string model)
	{
		ChatService.SetModel(model);
		StateHasChanged();
	}

	private void HandleTemperatureChange(float temp)
	{
		ChatService.SetTemperature(temp);
		StateHasChanged();
	}

	private void HandleMaxContextChange(int chars)
	{
		ChatService.SetMaxContextChars(chars);
		StateHasChanged();
	}

	private void HandleStreamChange(bool stream)
	{
		ChatService.SetStreamResponses(stream);
		StateHasChanged();
	}

	private void HandleChatStyleChange(ChatStyle style)
	{
		ChatStyle = style;
		StateHasChanged();
	}

	private async Task CopyMessage(AiMessage message)
	{
		try
		{
			await JS.InvokeVoidAsync("navigator.clipboard.writeText", message.Content);
			_copiedMessageId = message.GetHashCode().ToString();
			StateHasChanged();

			await Task.Delay(2000);
			if (_copiedMessageId == message.GetHashCode().ToString())
			{
				_copiedMessageId = null;
				StateHasChanged();
			}
		}
		catch
		{
			// Clipboard API not available
		}
	}

	private void ClearConversation()
	{
		_messages.Clear();
		_lastContextChars = 0;
		_totalEstimatedTokens = 0;
		_editingMessageIndex = -1;
		_editText = "";
		StateHasChanged();
	}

	private async Task HandleKeyDown(KeyboardEventArgs e)
	{
		if (e.Key == "Enter" && !e.ShiftKey)
		{
			await SendMessage();
		}
	}

	private async Task ScrollToBottomAsync()
	{
		try
		{
			await JS.InvokeVoidAsync("eval",
				"document.querySelector('.moka-ai-messages')?.scrollTo(0, 999999)");
		}
		catch
		{
			// Ignore
		}
	}

	private static string FormatContent(string content)
	{
		if (string.IsNullOrEmpty(content))
		{
			return "";
		}

		return Markdown.ToHtml(content, MarkdownPipeline);
	}

	internal static string FormatContextSize(int chars)
	{
		if (chars < 1000)
		{
			return $"{chars} chars";
		}

		return $"{chars / 1000.0:F1}K chars";
	}

	internal static string FormatDuration(long ms)
	{
		if (ms < 1000)
		{
			return $"{ms}ms";
		}

		return $"{ms / 1000.0:F1}s";
	}

	internal static string FormatTokens(int tokens)
	{
		if (tokens < 1000)
		{
			return $"~{tokens} tokens";
		}

		return $"~{tokens / 1000.0:F1}K tokens";
	}
}
