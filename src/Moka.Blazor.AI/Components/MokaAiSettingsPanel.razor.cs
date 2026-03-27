using Microsoft.AspNetCore.Components;
using Moka.Blazor.AI.Models;

namespace Moka.Blazor.AI.Components;

/// <summary>
///     Settings dropdown panel for an AI chat panel.
///     Provides runtime configuration of model, temperature, context size, and streaming.
/// </summary>
public sealed partial class MokaAiSettingsPanel : ComponentBase
{
	[Parameter] public bool IsVisible { get; set; }
	[Parameter] public EventCallback OnDismiss { get; set; }

	[Parameter] public string Model { get; set; } = "";
	[Parameter] public EventCallback<string> ModelChanged { get; set; }

	[Parameter] public float Temperature { get; set; } = 0.3f;
	[Parameter] public EventCallback<float> TemperatureChanged { get; set; }

	[Parameter] public int MaxContextChars { get; set; } = 8000;
	[Parameter] public EventCallback<int> MaxContextCharsChanged { get; set; }

	[Parameter] public bool StreamResponses { get; set; } = true;
	[Parameter] public EventCallback<bool> StreamResponsesChanged { get; set; }

	[Parameter] public ChatStyle ChatStyle { get; set; } = ChatStyle.Bubble;
	[Parameter] public EventCallback<ChatStyle> ChatStyleChanged { get; set; }

	[Parameter] public string ConnectionStatus { get; set; } = "unknown";

	private string TemperatureLabel => Temperature switch
	{
		< 0.2f => "Precise",
		< 0.5f => "Balanced",
		< 0.8f => "Creative",
		_ => "Wild"
	};

	private async Task HandleChatStyleChange(ChangeEventArgs e)
	{
		if (Enum.TryParse(e.Value?.ToString(), out ChatStyle style))
		{
			await ChatStyleChanged.InvokeAsync(style);
		}
	}

	private async Task HandleModelChange(ChangeEventArgs e)
	{
		string val = e.Value?.ToString() ?? "";
		if (!string.IsNullOrWhiteSpace(val))
		{
			await ModelChanged.InvokeAsync(val.Trim());
		}
	}

	private async Task HandleTemperatureChange(ChangeEventArgs e)
	{
		if (float.TryParse(e.Value?.ToString(), out float val))
		{
			await TemperatureChanged.InvokeAsync(Math.Clamp(val, 0f, 2f));
		}
	}

	private async Task HandleMaxContextChange(ChangeEventArgs e)
	{
		if (int.TryParse(e.Value?.ToString(), out int val))
		{
			await MaxContextCharsChanged.InvokeAsync(Math.Clamp(val, 500, 100000));
		}
	}
}
