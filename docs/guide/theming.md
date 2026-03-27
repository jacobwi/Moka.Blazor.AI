---
title: Theming
---

# Theming

## Built-in Themes

Set the theme via the `ThemeAttribute` parameter:

```razor
<MokaAiPanel ThemeAttribute="dark" />
```

| Value | Description |
|-------|-------------|
| `""` (empty) | Inherits from parent or uses system default |
| `"light"` | Light theme |
| `"dark"` | Dark theme |
| `"auto"` | Follows `prefers-color-scheme` media query |

## CSS Variables

All visual properties are controlled via CSS custom properties under `[data-moka-ai-theme]`:

```css
[data-moka-ai-theme="dark"] {
    --moka-ai-color-bg: #1e1e2e;
    --moka-ai-color-text: #cdd6f4;
    --moka-ai-color-border: #45475a;
    --moka-ai-color-hover: #313244;
    --moka-ai-color-primary: #0ea5e9;
    --moka-ai-color-muted: #6c7086;
}
```

### Available Variables

| Variable | Description | Default (Light) |
|----------|-------------|-----------------|
| `--moka-ai-color-bg` | Panel background | `#ffffff` |
| `--moka-ai-color-text` | Primary text color | `#1e293b` |
| `--moka-ai-color-border` | Border color | `#e2e8f0` |
| `--moka-ai-color-hover` | Hover/active background | `#f0f2f5` |
| `--moka-ai-color-primary` | Accent color (send button, links) | `#0ea5e9` |
| `--moka-ai-color-muted` | Secondary text (timestamps, labels) | `#94a3b8` |
| `--moka-ai-font-family` | Font family | `system-ui, sans-serif` |
| `--moka-ai-font-size` | Base font size | `13px` |
| `--moka-ai-radius` | Border radius | `8px` |

## Custom Themes

Create a fully custom theme by setting a custom attribute value and defining your CSS variables:

```razor
<MokaAiPanel ThemeAttribute="ocean" />
```

```css
[data-moka-ai-theme="ocean"] {
    --moka-ai-color-bg: #0a192f;
    --moka-ai-color-text: #e6f1ff;
    --moka-ai-color-border: #1d3557;
    --moka-ai-color-hover: #172a45;
    --moka-ai-color-primary: #64ffda;
    --moka-ai-color-muted: #8892b0;
}
```

## Chat Style Theming

Each chat style (Bubble, Classic, Compact) uses the same CSS variables, so custom themes apply consistently across all styles. Style-specific overrides use the `.moka-ai-style-bubble`, `.moka-ai-style-classic`, and `.moka-ai-style-compact` class selectors.
