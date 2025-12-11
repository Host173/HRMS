# HRMS Premium Design System Documentation

## Overview
This document outlines the implementation details for the HRMS Premium Corporate Theme. The design system focuses on a clean, professional aesthetic with subtle motion effects and a high-contrast color palette.

## 1. Design Tokens (CSS Variables)

The core design values are defined in `:root` within `wwwroot/css/premium-theme.css`.

### Colors
- **Primary Blue**: `var(--color-primary)` (#2463EB)
- **Dark Background**: `var(--color-bg-dark)` (#0F172A)
- **Light Surface**: `var(--color-surface)` (#FFFFFF)
- **Text**: `var(--color-text-primary)` (#0F172A)
- **Success**: `var(--color-success)` (#22C55E)
- **Danger**: `var(--color-danger)` (#EF4444)

### Spacing & Layout
- **Border Radius**: `var(--radius-sm/md/lg/xl)`
- **Shadows**: `var(--shadow-sm/md/lg)`
- **Transitions**: `var(--transition-fast/normal/slow)`

## 2. Animation System

The theme uses pure CSS animations for performance and smoothness.

### LED Glow Effect
Used on logos and active elements.
```css
.logo-container {
    animation: led-breathe 6s ease-in-out infinite;
}
```
**Mechanism**: Cycles through scale (1.0 to 1.02) and drop-shadow intensity to simulate a "breathing" LED light.

### Background Drift
Used on the dark split-screen panel.
```css
.bg-premium-dark {
    animation: bg-drift 15s ease infinite;
}
```
**Mechanism**: Slowly pans a large gradient background to create a subtle, floating effect.

### Fade In
Used for content entry.
```css
.animate-fade-in {
    animation: fade-in-up 0.6s cubic-bezier(...) forwards;
}
```

## 3. Component Usage

### Buttons
Use `.btn-premium` as the base class.
- **Primary**: `.btn-premium .btn-primary`
- **Secondary**: `.btn-premium .btn-secondary`

```html
<button class="btn-premium btn-primary">Action</button>
```

### Cards
Use `.premium-card` for elevated content containers.
```html
<div class="premium-card">
    <h2>Title</h2>
    <p>Content...</p>
</div>
```

### Forms
Standard form structure with enhanced styling.
```html
<div class="form-group">
    <label class="form-label">Label</label>
    <input class="form-control" type="text" />
</div>
```

## 4. Layout Structures

### Split Screen (Login/Register)
The layout divides the screen into a dark left panel and a light right panel.

**Implementation**:
1. Set `ViewData["HideLayout"] = true` in the view.
2. Set `Layout = "~/Views/Shared/_Layout.cshtml"`.
3. Wrap content in `.split-layout`.

```html
<div class="split-layout">
    <div class="split-left bg-premium-dark">
        <!-- Brand/Marketing Content -->
    </div>
    <div class="split-right">
        <!-- Form Content -->
    </div>
</div>
```

### Dashboard (Main App)
The default layout (`_Layout.cshtml`) provides a premium navbar and centered content area. No special classes needed for the main container; it handles itself.

## 5. Extending the System

To add new components:
1. Define new variables in `:root` if needed (e.g., new colors).
2. Create component classes in `premium-theme.css`.
3. Use existing utility variables (spacing, shadows) to maintain consistency.

## 6. Best Practices

- **Consistency**: Always use the defined CSS variables for colors and spacing.
- **Motion**: Keep animations subtle. Use `var(--transition-normal)` for interactive states.
- **Accessibility**: Semantic HTML is preserved. High contrast ratios are maintained in the color palette.

