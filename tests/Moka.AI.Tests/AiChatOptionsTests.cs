using Moka.AI.Models;
using Xunit;

namespace Moka.AI.Tests;

public sealed class AiChatOptionsTests
{
	[Fact]
	public void Defaults_AreExpected()
	{
		var options = new AiChatOptions();

		Assert.Equal(AiProvider.OpenAiCompatible, options.Provider);
		Assert.Null(options.Endpoint);
		Assert.Null(options.ApiKey);
		Assert.Equal("local-model", options.DefaultModel);
		Assert.Null(options.SystemPrompt);
		Assert.Equal(8000, options.MaxContextChars);
		Assert.True(options.StreamResponses);
		Assert.Equal(0.3f, options.Temperature);
	}

	[Theory]
	[InlineData(AiProvider.OpenAiCompatible, "http://localhost:1234/v1")]
	[InlineData(AiProvider.Ollama, "http://localhost:11434")]
	[InlineData(AiProvider.Onnx, "")]
	public void ResolvedEndpoint_UsesProviderDefault_WhenEndpointNull(AiProvider provider, string expected)
	{
		var options = new AiChatOptions { Provider = provider };

		Assert.Equal(expected, options.ResolvedEndpoint);
	}

	[Fact]
	public void ResolvedEndpoint_PrefersExplicitEndpoint()
	{
		var options = new AiChatOptions { Endpoint = "http://example.com/v1" };

		Assert.Equal("http://example.com/v1", options.ResolvedEndpoint);
	}
}
