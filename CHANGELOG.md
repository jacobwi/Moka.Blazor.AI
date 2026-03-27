# Changelog

## [0.1.0] - 2026-03-27

### ✨ New
- **MokaAiPanel** — reusable Blazor chat panel component with streaming responses, editable messages, and pluggable AI providers
- **Three chat styles** — Bubble (modern), Classic (flat), Compact (minimal) — switchable at runtime from the settings panel
- **Streaming first** — token-by-token streaming via `IAsyncEnumerable` with stop/cancel support
- **Edit & re-send** — edit any previous user message and re-send from that point in the conversation
- **Settings panel** — model, temperature, max context size, streaming toggle, chat style
- **Quick actions** — configurable prompt buttons (e.g., Summarize, Analyze)
- **Markdown rendering** — assistant responses rendered as Markdown via Markdig
- **Token & context tracking** — estimated token count and context window usage
- **Connection status** — auto-detect backend availability with retry
- **Provider support** — OpenAI-compatible (LM Studio, vLLM), Ollama, or any custom `IChatClient`
- **Theming** — light, dark, and auto modes via CSS variables and `[data-moka-ai-theme]` attribute
- **Multi-target** — supports .NET 9 and .NET 10

### ✨ New (Moka.Blazor.AI.Onnx)
- **ONNX Runtime GenAI** companion package for fully embedded/offline AI models
- Registers `OnnxRuntimeGenAIChatClient` as `IChatClient` — no external server required
- Hardware acceleration options: CPU, CUDA, DirectML

[0.1.0]: https://github.com/jacobwi/Moka.Blazor.AI/releases/tag/v0.1.0
