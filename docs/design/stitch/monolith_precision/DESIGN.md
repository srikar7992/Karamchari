---
name: Monolith Precision
colors:
  surface: '#131313'
  surface-dim: '#131313'
  surface-bright: '#393939'
  surface-container-lowest: '#0e0e0e'
  surface-container-low: '#1b1b1b'
  surface-container: '#1f1f1f'
  surface-container-high: '#2a2a2a'
  surface-container-highest: '#353535'
  on-surface: '#e2e2e2'
  on-surface-variant: '#c4c7c8'
  inverse-surface: '#e2e2e2'
  inverse-on-surface: '#303030'
  outline: '#8e9192'
  outline-variant: '#444748'
  surface-tint: '#c6c6c7'
  primary: '#ffffff'
  on-primary: '#2f3131'
  primary-container: '#e2e2e2'
  on-primary-container: '#636565'
  inverse-primary: '#5d5f5f'
  secondary: '#b7c8e1'
  on-secondary: '#213145'
  secondary-container: '#3a4a5f'
  on-secondary-container: '#a9bad3'
  tertiary: '#ffffff'
  on-tertiary: '#2f3131'
  tertiary-container: '#e2e2e2'
  on-tertiary-container: '#636565'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#e2e2e2'
  primary-fixed-dim: '#c6c6c7'
  on-primary-fixed: '#1a1c1c'
  on-primary-fixed-variant: '#454747'
  secondary-fixed: '#d3e4fe'
  secondary-fixed-dim: '#b7c8e1'
  on-secondary-fixed: '#0b1c30'
  on-secondary-fixed-variant: '#38485d'
  tertiary-fixed: '#e2e2e2'
  tertiary-fixed-dim: '#c6c6c7'
  on-tertiary-fixed: '#1a1c1c'
  on-tertiary-fixed-variant: '#454747'
  background: '#131313'
  on-background: '#e2e2e2'
  surface-variant: '#353535'
typography:
  display:
    fontFamily: Inter
    fontSize: 48px
    fontWeight: '600'
    lineHeight: '1.1'
    letterSpacing: -0.04em
  h1:
    fontFamily: Inter
    fontSize: 32px
    fontWeight: '600'
    lineHeight: '1.2'
    letterSpacing: -0.03em
  h2:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '500'
    lineHeight: '1.3'
    letterSpacing: -0.02em
  body-large:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '400'
    lineHeight: '1.6'
    letterSpacing: -0.01em
  body-base:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: '1.5'
    letterSpacing: 0em
  label-caps:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '600'
    lineHeight: '1'
    letterSpacing: 0.05em
  mono:
    fontFamily: monospace
    fontSize: 13px
    fontWeight: '400'
    lineHeight: '1.4'
    letterSpacing: 0em
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  xs: 0.25rem
  sm: 0.5rem
  md: 1rem
  lg: 1.5rem
  xl: 2.5rem
  gutter: 1.5rem
  margin: 2rem
---

## Brand & Style

This design system is built on the principles of **Hyper-Minimalism** and **Signal-to-Noise Optimization**. Designed for high-stakes enterprise management, the brand personality is clinical, authoritative, and invisible. It prioritizes the "Work" over the "UI," ensuring that data and actions are the primary focus.

The aesthetic draws from the engineering-led design languages of Vercel and Linear, utilizing a monochromatic foundation to convey stability and technical prowess. There is zero decorative clutter; every line, pixel, and whitespace interval serves a functional purpose. The emotional response should be one of extreme efficiency and "calm control."

## Colors

The palette is strictly monochromatic to eliminate visual distraction. We utilize a "Pure Dark" approach where the background is true black (#000000) to maximize contrast with typography and minimize hardware power consumption on OLED displays.

- **Primary:** Pure White is reserved for primary actions, headings, and active states.
- **Secondary:** Slate/Muted Grays are used for secondary text and non-interactive icons to create a clear hierarchy.
- **Semantic:** Colors are used only as data indicators. Green, Red, and Amber are never used for branding; they are reserved exclusively for status (Active, Critical, Warning).
- **Borders:** Borders use a hairline thickness with a 10% white opacity, creating a "ghost" boundary that disappears into the background.

## Typography

This design system leverages **Inter** for its neutral, systematic clarity. Typography is the primary driver of hierarchy in the absence of heavy background colors. 

- **Weight as Hierarchy:** Use Semibold (600) for headers to anchor the page, and Regular (400) for all data entry. 
- **Micro-type:** Label-caps are used for section headers in sidebars or small data labels to provide a distinct structural "break" without adding weight.
- **Monospace:** For ID tags, employee codes, or financial figures, use a system monospace font to imply precision and technical accuracy.

## Layout & Spacing

The layout philosophy is based on **Structural Whitespace**. Instead of using cards or different background shades to group content, we use exaggerated margins and precise alignment. 

- **Grid:** A 12-column fluid grid is used for main dashboard views. 
- **Alignment:** Strict adherence to a 4px baseline grid. Components must align to the edges of the grid to maintain an "engineered" look.
- **Sectioning:** Vertical spacing (2.5rem+) is preferred over horizontal dividers. Dividers should only be used when content is so dense that whitespace alone cannot provide enough separation.

## Elevation & Depth

This system avoids traditional shadows in favor of **Tonal Layering** and **Low-Contrast Outlines**.

- **Surface Levels:** Level 0 is #000000. Level 1 (Modals/Popovers) uses a slightly elevated dark gray (#0A0A0A) or a subtle backdrop blur (20px) to imply depth.
- **Depth via Borders:** Instead of shadows, we use a 1px solid border (`rgba(255,255,255,0.1)`) to define the perimeter of elevated elements.
- **Active States:** Elevation is often indicated by a shift from a muted border to a pure white border, or a subtle "inner glow" effect for buttons.

## Shapes

The shape language is "Soft-Precision." We avoid sharp 0px corners to prevent a dated "Brutalist" feel, opting instead for a subtle 4px (0.25rem) radius. 

- **Consistency:** All interactive elements—inputs, buttons, and cards—must share the same `rounded-sm` radius. 
- **Outer vs Inner:** When nesting elements, the inner radius should be half the outer radius to maintain geometric harmony.

## Components

- **Buttons:** 
  - *Primary:* Solid White background with Black text. No shadow.
  - *Secondary:* Transparent background with a 1px white border (10% opacity).
  - *Ghost:* No background or border; turns white on hover.
- **Input Fields:** Minimalist under-lines or subtle boxes. Focus state is a 1px white border. Placeholder text should be significantly muted (Slate-500).
- **Chips:** Small, rectangular with a 2px radius. Monochromatic unless indicating a specific status (e.g., "Active" uses a small green dot icon next to white text).
- **Lists:** High-density with subtle 1px dividers. Hover states should trigger a very subtle background shift (#0A0A0A).
- **Data Tables:** No vertical lines. Horizontal lines should be the "border-border" color. Header cells use `label-caps` typography.
- **Enterprise Specifics:**
  - *Command Palette:* A central feature of the UI. Should be a floating modal with a heavy backdrop blur.
  - *Status Indicators:* Use "Small Dots" (8px) with semantic colors instead of large badges to maintain the high signal-to-noise ratio.