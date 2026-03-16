# Design Tokens - Bud 2.0

This document describes the design token system implemented in the Bud application, sourced from the `mdonangelo/bud-2-design-system` repository (React + CSS Modules).

## Overview

Design tokens are the visual design atoms of the design system — specifically, they are named entities that store visual design attributes. We use them in place of hard-coded values (like hex values for color or pixels for spacing) to maintain a scalable and consistent visual system.

The DS repo is the **source of truth**. When tokens change there, this project updates to match.

## Source

**DS Repository:** `mdonangelo/bud-2-design-system` (React + CSS Modules)
**Figma Style Guide:** [Bud 2.0 Style Guide on Figma](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide)

## Token Naming Convention

The DS uses a consistent naming pattern:

```
--color-{palette}-{shade}   (primitive colors)
--font-{role}                (font families)
--text-{scale}               (font sizes, rem-based)
--sp-{token}                 (spacing)
--radius-{token}             (border radius)
--shadow-{size}              (shadows)
```

### Backward Compatibility

Old token names (e.g. `--orange-500`, `--font-family-body`, `--spacing-4`) are defined as aliases pointing to the new DS names. This allows gradual migration without breaking existing code.

## Token Structure

Design tokens are defined in [`src/Client/Bud.BlazorWasm/wwwroot/css/tokens.css`](src/Client/Bud.BlazorWasm/wwwroot/css/tokens.css).

### 1. Primitive Colors

Base color scales (50-950) for each color family, prefixed with `--color-`:

```css
/* Orange - Primary Brand Color */
--color-orange-50: #fff4ed;
--color-orange-500: #fd5f28;  /* Primary Orange */
--color-orange-950: #440e07;

/* Wine - Secondary Brand Color */
--color-wine-50: #fef1f7;
--color-wine-500: #fa3a82;  /* Primary Wine */
--color-wine-800: #a60c46;
--color-wine-950: #560121;

/* Neutral - Grayscale */
--color-neutral-50: #fafafa;
--color-neutral-500: #737373;
--color-neutral-950: #0a0a0a;
```

Other color families: `caramel`, `red`, `green`, `yellow`

### 2. Semantic Colors

Meaningful color assignments mapped from primitives:

```css
/* Brand Colors */
--color-brand-primary: var(--color-orange-500);
--color-brand-secondary: var(--color-wine-500);

/* Text Colors */
--color-text-primary: var(--color-neutral-950);
--color-text-muted: var(--color-neutral-500);
--color-text-inverse: #ffffff;

/* Border Colors (caramel-based) */
--color-border: var(--color-caramel-200);
--color-border-light: var(--color-caramel-100);
--color-border-focus: var(--color-caramel-700);

/* State Colors */
--color-success: var(--color-green-500);
--color-error: var(--color-red-500);
--color-warning: var(--color-yellow-500);
--color-info: var(--color-wine-500);
```

### 3. Typography

Three font families sourced from the DS:

```css
/* Font Families */
--font-display: 'Crimson Pro', Georgia, serif;         /* Display/decorative */
--font-heading: 'Plus Jakarta Sans', sans-serif;        /* Headings */
--font-body: 'Inter', sans-serif;                       /* Body text (default) */
--font-mono: 'SFMono-Regular', Consolas, monospace;     /* Code */

/* Font Sizes (rem-based) */
--text-8xl: 4rem;       /* 64px */
--text-7xl: 3.25rem;    /* 52px */
--text-6xl: 2.75rem;    /* 44px */
--text-5xl: 2.25rem;    /* 36px */
--text-4xl: 2rem;       /* 32px */
--text-3xl: 1.75rem;    /* 28px */
--text-2xl: 1.5rem;     /* 24px */
--text-xl: 1.25rem;     /* 20px */
--text-lg: 1.125rem;    /* 18px */
--text-base: 1rem;      /* 16px */
--text-sm: 0.875rem;    /* 14px */
--text-xs: 0.75rem;     /* 12px */

/* Font Weights */
--font-weight-regular: 400;
--font-weight-medium: 500;
--font-weight-semibold: 600;
--font-weight-bold: 700;
```

### 4. Spacing

DS spacing scale using `--sp-` prefix:

```css
--sp-3xs: 0.25rem;   /* 4px */
--sp-2xs: 0.5rem;    /* 8px */
--sp-xs: 0.75rem;    /* 12px */
--sp-sm: 1rem;       /* 16px */
--sp-md: 1.25rem;    /* 20px */
--sp-lg: 1.5rem;     /* 24px */
--sp-xl: 2rem;       /* 32px */
--sp-2xl: 2.5rem;    /* 40px */
--sp-3xl: 3rem;      /* 48px */
--sp-4xl: 4rem;      /* 64px */
```

Legacy `--spacing-N` aliases (pixel-based) are preserved for backward compatibility.

### 5. Border Radius

```css
--radius-2xs: 2px;
--radius-xs: 4px;
--radius-sm: 6px;
--radius-md: 8px;
--radius-lg: 12px;
--radius-full: 9999px;  /* Pill shape */
```

### 6. Shadows

Warm-brown base (`rgba(24, 18, 12, ...)`) per DS spec:

```css
--shadow-xs: 0 1px 2px 0 rgba(24, 18, 12, 0.05);
--shadow-sm: 0 1px 3px 0 rgba(24, 18, 12, 0.1), 0 1px 2px 0 rgba(24, 18, 12, 0.06);
--shadow-md: 0 10px 15px -3px rgba(24, 18, 12, 0.1), 0 4px 6px -2px rgba(24, 18, 12, 0.05);
--shadow-card: var(--shadow-xs);
```

### 7. Layout

```css
--sidebar-width: 240px;
--sidebar-width-collapsed: 56px;
--header-height: 64px;
--grid-gap: 24px;
```

## CSS Architecture

```
wwwroot/css/
  reset.css       ← Box-sizing reset, font smoothing
  fonts.css       ← @font-face for Crimson Pro, Plus Jakarta Sans, Inter
  tokens.css      ← Design tokens (DS source of truth + backward aliases)
  app.css         ← All component and page styles
```

Load order in `index.html`:
```html
<link rel="stylesheet" href="css/reset.css" />
<link rel="stylesheet" href="css/fonts.css" />
<link rel="stylesheet" href="css/tokens.css" />
<link rel="stylesheet" href="css/app.css" />
```

## Font Loading

Three font families self-hosted in `wwwroot/fonts/`:

- **Crimson Pro** (`crimson-pro-latin.woff2`): Display/decorative text
- **Plus Jakarta Sans** (`plus-jakarta-sans-latin.woff2`): Headings
- **Inter** (`inter-latin.woff2`): Body text and UI elements

## Color Palette Reference

### Brand Colors

| Token | Hex | Usage |
|-------|-----|-------|
| `--color-orange-500` | #fd5f28 | Primary brand color, CTAs, active states |
| `--color-wine-500` | #fa3a82 | Secondary brand color, accents |
| `--color-wine-800` | #a60c46 | Dark wine, used in dense contexts |

### Neutral Scale

| Token | Hex | Usage |
|-------|-----|-------|
| `--color-neutral-100` | #f5f5f5 | Page background |
| `--color-neutral-500` | #737373 | Muted text, placeholders |
| `--color-neutral-950` | #0a0a0a | Primary text |

### Semantic Colors

| Token | Hex | Usage |
|-------|-----|-------|
| `--color-green-500` | #22c55e | Success messages, positive states |
| `--color-red-500` | #ef4444 | Error messages, danger states |
| `--color-yellow-500` | #eab308 | Warning messages, caution states |

## Updating Tokens from DS Repo

When the `mdonangelo/bud-2-design-system` repo updates tokens:

1. Compare new values with `tokens.css`
2. Update primitive and semantic values
3. Verify backward-compat aliases still resolve correctly
4. Run `docker compose up --build` and visually test all pages
5. Run `dotnet test` to verify no test regressions

## Best Practices

1. **Always use tokens**: Never hardcode colors, spacing, or typography values
2. **Use semantic tokens**: Prefer `--color-text-primary` over `--color-neutral-950`
3. **New code uses DS names**: Prefer `--color-orange-500` over `--orange-500`, `--sp-sm` over `--spacing-4`
4. **Document changes**: Update this file when adding new tokens
5. **Test thoroughly**: Check all pages after token updates

---

**Last Updated:** 2026-03-15
**Source of Truth:** `mdonangelo/bud-2-design-system`
**Figma Style Guide:** [View on Figma](https://www.figma.com/design/j3n8YHBusCH8KEHvheGeF8/-ASSETS--Style-Guide)
