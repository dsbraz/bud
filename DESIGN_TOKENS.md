# Design Tokens - Bud 2.0

This document describes the design token system implemented in the Bud application, based on the Figma Style Guide.

## Overview

Design tokens are the visual design atoms of the design system — specifically, they are named entities that store visual design attributes. We use them in place of hard-coded values (like hex values for color or pixels for spacing) to maintain a scalable and consistent visual system.

## Figma Style Guide

**Source:** [Bud 2.0 Style Guide on Figma](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide)

The style guide is organized into the following sections:

### Foundation
- **Colors**: [Primitives](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=37-684) - Complete color palette with Orange, Wine, Caramel, Neutral, Red, Green, Yellow
- **Typography**: [Estilos tipográficos](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=34-11) - Crimson Pro and Plus Jakarta Sans
- **Logos**: [Brand logos](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=45-7412) - Primary and secondary logos
- **Icons**: [Phosphor Icons 2.1](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=46-7595)

### Components
- **Buttons**: [Button styles](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=144-1509)
- **Inputs**: [Input fields](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=159-6)
- **Checkboxes**: [Checkbox components](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=199-562)
- **Badges**: [Badge variants](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=234-3825)
- **Loading**: [Loading states](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide?node-id=164-1037)

## Token Structure

Design tokens are defined in [`src/Bud.Client/wwwroot/css/tokens.css`](src/Bud.Client/wwwroot/css/tokens.css) and organized into the following categories:

### 1. Primitive Colors

Base color scales (50-950) for each color family:

```css
/* Orange - Primary Brand Color */
--orange-50: #FFF4ED;
--orange-500: #FF6B35;  /* Primary Orange */
--orange-950: #4A1505;

/* Wine - Secondary Brand Color */
--wine-50: #FDF3F8;
--wine-500: #E838A3;  /* Primary Wine */
--wine-950: #3A0824;

/* Neutral - Grayscale */
--neutral-50: #FAFAFA;
--neutral-500: #737373;
--neutral-950: #0A0A0A;
```

Other color families: `caramel`, `red`, `green`, `yellow`

### 2. Semantic Colors

Meaningful color assignments mapped from primitives:

```css
/* Brand Colors */
--color-brand-primary: var(--orange-500);
--color-brand-secondary: var(--wine-500);

/* Text Colors */
--color-text-primary: var(--neutral-900);
--color-text-muted: var(--neutral-500);
--color-text-inverse: #FFFFFF;

/* State Colors */
--color-success: var(--green-500);
--color-error: var(--red-500);
--color-warning: var(--yellow-500);
--color-info: var(--wine-500);
```

### 3. Typography

Font families, sizes, weights, and spacing:

```css
/* Font Families */
--font-family-display: 'Crimson Pro', Georgia, 'Times New Roman', serif;
--font-family-body: 'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;

/* Font Sizes - Display */
--font-size-display-xl: 64px;
--font-size-display-lg: 52px;
--font-size-display-md: 44px;
--font-size-display-sm: 36px;

/* Font Weights */
--font-weight-regular: 400;
--font-weight-medium: 500;
--font-weight-semibold: 600;
--font-weight-bold: 700;
```

### 4. Spacing

8px-based spacing scale:

```css
--spacing-1: 4px;
--spacing-2: 8px;
--spacing-4: 16px;
--spacing-6: 24px;
--spacing-8: 32px;

/* Semantic Aliases */
--spacing-xs: var(--spacing-1);
--spacing-sm: var(--spacing-2);
--spacing-md: var(--spacing-4);
--spacing-lg: var(--spacing-6);
--spacing-xl: var(--spacing-8);
```

### 5. Border Radius

```css
--radius-sm: 4px;
--radius-md: 8px;
--radius-lg: 12px;
--radius-xl: 16px;
--radius-full: 9999px;  /* Pill shape */
```

### 6. Shadows

```css
--shadow-sm: 0 1px 3px 0 rgba(0, 0, 0, 0.1), 0 1px 2px 0 rgba(0, 0, 0, 0.06);
--shadow-md: 0 10px 15px -3px rgba(0, 0, 0, 0.1), 0 4px 6px -2px rgba(0, 0, 0, 0.05);
--shadow-card: var(--shadow-sm);
```

### 7. Layout

```css
--sidebar-width: 280px;
--header-height: 64px;
--grid-gap: 24px;
```

## Token Naming Convention

Tokens follow a consistent naming pattern:

```
--{category}-{property}-{variant}-{state}
```

**Examples:**
- `--color-text-primary` - Text color, primary variant
- `--font-size-title-lg` - Font size for titles, large variant
- `--spacing-md` - Medium spacing
- `--shadow-card` - Shadow for card components

## Using Tokens in Code

### In CSS

Replace hardcoded values with token references:

```css
/* ❌ Before */
.button {
    background: #FF6B35;
    padding: 10px 16px;
    border-radius: 10px;
}

/* ✅ After */
.button {
    background: var(--color-brand-primary);
    padding: var(--spacing-3) var(--spacing-4);
    border-radius: var(--radius-md);
}
```

### In Blazor Components

Tokens are automatically available in all components via the cascading CSS:

```razor
<div style="color: var(--color-text-muted); font-size: var(--font-size-sm);">
    Muted text
</div>
```

For component-specific styles, create a `.razor.css` file:

```css
/* MyComponent.razor.css */
.container {
    padding: var(--spacing-6);
    background: var(--color-surface);
    border-radius: var(--radius-lg);
}
```

## Updating Tokens from Figma

There are two methods to update design tokens when the Figma Style Guide changes:

### Method 1: MCP-Powered Sync (Recommended)

If you have Figma MCP configured, you can use Claude Code to fetch updated tokens:

1. **Prerequisites:**
   - Figma MCP server configured (see MCP Setup section below)
   - Figma Desktop app open with the Style Guide file

2. **Update Process:**
   ```bash
   # Open Claude Code and ask:
   "Check the Figma Style Guide for design token changes and update tokens.css"
   ```

3. **Review Changes:**
   ```bash
   git diff src/Bud.Client/wwwroot/css/tokens.css
   ```

4. **Test and Commit:**
   - Run `docker compose up --build`
   - Test all pages
   - Commit if approved

### Method 2: Manual Export via Figma Plugin

1. **Install Plugin:**
   - Open [Figma Token Exporter](https://www.figma.com/community/plugin/1345069854741911632/figma-token-exporter) in Figma

2. **Export Tokens:**
   - Open the Bud 2.0 Style Guide in Figma
   - Run the Figma Token Exporter plugin
   - Select **CSS Custom Properties** format
   - Download the exported `.css` file

3. **Update tokens.css:**
   - Compare the exported file with [`src/Bud.Client/wwwroot/css/tokens.css`](src/Bud.Client/wwwroot/css/tokens.css)
   - Update changed values
   - Maintain the existing file structure and comments

4. **Test and Commit:**
   - Run `docker compose up --build`
   - Test all pages
   - Commit changes

## MCP Setup

To use Figma MCP integration for automated token updates:

### Option 1: Remote MCP Server (Recommended)

```bash
# Add Figma MCP to Claude Code
claude mcp add --transport http figma https://mcp.figma.com/mcp

# Authenticate
# 1. Type /mcp in Claude Code
# 2. Select "figma" from MCP Servers
# 3. Click "Authenticate"
# 4. Allow access in browser
```

### Option 2: Desktop MCP Server

1. Open Figma Desktop app
2. Navigate to any file
3. Open Inspect panel
4. Enable "Desktop MCP server"
5. Server runs at `http://127.0.0.1:3845/mcp`

## Token Update Workflow

When a designer updates the Figma Style Guide:

1. **Notification**: Designer notifies team of Figma updates
2. **Sync**: Developer runs MCP sync or manual export
3. **Review**: Check `git diff` for changes
4. **Test**: Visual regression testing on all pages
5. **Approve**: Team reviews visual changes
6. **Deploy**: Merge and deploy to production

## Font Loading

The application uses two font families from Google Fonts:

- **Crimson Pro**: For headings and display text
- **Plus Jakarta Sans**: For body text and UI elements

Fonts should be loaded in [`src/Bud.Client/wwwroot/index.html`](src/Bud.Client/wwwroot/index.html):

```html
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Crimson+Pro:wght@400;600;700&family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap" rel="stylesheet">
```

## Color Palette Reference

### Brand Colors

| Token | Hex | Usage |
|-------|-----|-------|
| `--orange-500` | #FF6B35 | Primary brand color, CTAs, active states |
| `--wine-500` | #E838A3 | Secondary brand color, accents, highlights |

### Neutral Scale

| Token | Hex | Usage |
|-------|-----|-------|
| `--neutral-50` | #FAFAFA | Background, page background |
| `--neutral-100` | #F5F5F5 | Subtle backgrounds, hover states |
| `--neutral-300` | #D4D4D4 | Borders, dividers |
| `--neutral-500` | #737373 | Muted text, placeholders |
| `--neutral-700` | #404040 | Secondary text |
| `--neutral-900` | #171717 | Primary text |

### Semantic Colors

| Token | Hex | Usage |
|-------|-----|-------|
| `--green-500` | #22C55E | Success messages, positive states |
| `--red-500` | #EF4444 | Error messages, danger states |
| `--yellow-500` | #EAB308 | Warning messages, caution states |

## Typography Scale

### Display (Crimson Pro)

| Token | Size | Usage |
|-------|------|-------|
| `--font-size-display-xl` | 64px | Hero headlines |
| `--font-size-display-lg` | 52px | Page headlines |
| `--font-size-display-md` | 44px | Section headlines |
| `--font-size-display-sm` | 36px | Subsection headlines |

### Titles (Crimson Pro)

| Token | Size | Usage |
|-------|------|-------|
| `--font-size-title-xl` | 32px | Main page titles |
| `--font-size-title-lg` | 28px | Section titles |
| `--font-size-title-md` | 24px | Subsection titles |
| `--font-size-title-sm` | 20px | Card titles |

### Body (Plus Jakarta Sans)

| Token | Size | Usage |
|-------|------|-------|
| `--font-size-paragraph-lg` | 18px | Lead paragraphs |
| `--font-size-base` | 14px | Body text, inputs |
| `--font-size-label-md` | 13px | Form labels, captions |
| `--font-size-label-sm` | 12px | Small text, metadata |

## Spacing Scale

| Token | Value | Usage |
|-------|-------|-------|
| `--spacing-1` | 4px | Tight spacing, icon gaps |
| `--spacing-2` | 8px | Small gaps, form elements |
| `--spacing-3` | 12px | Button padding, small cards |
| `--spacing-4` | 16px | Default spacing, margins |
| `--spacing-6` | 24px | Section spacing, card padding |
| `--spacing-8` | 32px | Large spacing, page margins |
| `--spacing-10` | 40px | Extra large spacing |

## Browser Support

Design tokens using CSS Custom Properties are supported in:

- Chrome 49+
- Firefox 31+
- Safari 9.1+
- Edge 15+

For older browsers, consider using a CSS preprocessor to compile tokens at build time.

## Best Practices

1. **Always use tokens**: Never hardcode colors, spacing, or typography values
2. **Use semantic tokens**: Prefer `--color-text-primary` over `--neutral-900`
3. **Maintain hierarchy**: Use primitive → semantic → component token structure
4. **Document changes**: Update this file when adding new tokens
5. **Test thoroughly**: Check all pages after token updates
6. **Keep in sync**: Regularly sync tokens with Figma Style Guide

## Troubleshooting

### Tokens not loading

Check that `tokens.css` is loaded before `app.css` in [`index.html`](src/Bud.Client/wwwroot/index.html):

```html
<link rel="stylesheet" href="css/tokens.css" />
<link rel="stylesheet" href="css/app.css" />
```

### Colors look wrong

1. Clear browser cache
2. Check browser DevTools → Computed styles
3. Verify token values in `tokens.css`
4. Check for typos in variable names

### Fonts not loading

1. Verify Google Fonts link in `index.html`
2. Check browser console for font loading errors
3. Verify font-family declarations in tokens.css

## Resources

- [Figma Style Guide](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide)
- [Figma MCP Documentation](https://developers.figma.com/docs/figma-mcp-server/)
- [CSS Custom Properties (MDN)](https://developer.mozilla.org/en-US/docs/Web/CSS/Using_CSS_custom_properties)
- [Design Tokens W3C Draft](https://design-tokens.github.io/community-group/format/)

## Contributing

When adding new components or pages:

1. **Use existing tokens**: Check if existing tokens fit your needs
2. **Request new tokens**: If needed, request addition to Figma Style Guide
3. **Update documentation**: Add new tokens to this document
4. **Follow conventions**: Use established naming patterns

## Version History

| Version | Date | Changes |
|---------|------|---------|
| 2.0.0 | 2026-02-02 | Initial design token system implementation |
| | | - Complete rebrand with Orange/Wine palette |
| | | - Crimson Pro + Plus Jakarta Sans typography |
| | | - Comprehensive spacing, shadow, and layout tokens |

---

**Last Updated:** 2026-02-02
**Maintained By:** Bud Development Team
**Figma Style Guide:** [View on Figma](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide)
