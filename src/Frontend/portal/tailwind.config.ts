import type { Config } from "tailwindcss";

/**
 * Tailwind config — derived from the Stitch UI's "Monolith Precision" design system
 * (see docs/design/stitch/monolith_precision/DESIGN.md). Pure dark, monochromatic,
 * Inter typography, hairline ghost borders, 4px corner radius.
 */
const config: Config = {
  darkMode: "class",
  content: ["./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ["var(--font-inter)", "system-ui", "sans-serif"],
        mono: ["ui-monospace", "SFMono-Regular", "Menlo", "Consolas", "monospace"],
      },
      colors: {
        // Surface scale — true black background, lightening as elevation rises.
        background: "#131313",
        surface: "#131313",
        "surface-dim": "#131313",
        "surface-bright": "#393939",
        "surface-container-lowest": "#0e0e0e",
        "surface-container-low": "#1b1b1b",
        "surface-container": "#1f1f1f",
        "surface-container-high": "#2a2a2a",
        "surface-container-highest": "#353535",
        "surface-variant": "#353535",
        "surface-tint": "#c6c6c7",

        // On-surface / typography scale.
        "on-surface": "#e2e2e2",
        "on-surface-variant": "#c4c7c8",
        "on-background": "#e2e2e2",

        // Outlines — hairline ghost borders.
        outline: "#8e9192",
        "outline-variant": "#444748",

        // Primary action: pure white reserved for primary actions only.
        primary: "#ffffff",
        "on-primary": "#2f3131",
        "primary-container": "#e2e2e2",
        "on-primary-container": "#636565",
        "inverse-primary": "#5d5f5f",
        "inverse-surface": "#e2e2e2",
        "inverse-on-surface": "#303030",

        // Secondary: muted slate for non-primary accents.
        secondary: "#b7c8e1",
        "on-secondary": "#213145",
        "secondary-container": "#3a4a5f",
        "on-secondary-container": "#a9bad3",

        // Semantic colours — used ONLY for status, never branding.
        error: "#ffb4ab",
        "on-error": "#690005",
        "error-container": "#93000a",
        "on-error-container": "#ffdad6",
        success: "#34d399",
        warning: "#fbbf24",
      },
      borderRadius: {
        DEFAULT: "0.25rem",
        sm: "0.125rem",
        md: "0.375rem",
        lg: "0.5rem",
        xl: "0.75rem",
      },
      spacing: {
        gutter: "1.5rem",
        margin: "2rem",
      },
      fontSize: {
        // From DESIGN.md typography scale.
        display: ["48px", { lineHeight: "1.1", letterSpacing: "-0.04em", fontWeight: "600" }],
        h1: ["32px", { lineHeight: "1.2", letterSpacing: "-0.03em", fontWeight: "600" }],
        h2: ["24px", { lineHeight: "1.3", letterSpacing: "-0.02em", fontWeight: "500" }],
        "body-lg": ["16px", { lineHeight: "1.6", letterSpacing: "-0.01em" }],
        body: ["14px", { lineHeight: "1.5", letterSpacing: "0em" }],
        caps: ["11px", { lineHeight: "1", letterSpacing: "0.05em", fontWeight: "600" }],
        mono: ["13px", { lineHeight: "1.4", letterSpacing: "0em", fontWeight: "400" }],
      },
      borderColor: {
        DEFAULT: "rgba(255, 255, 255, 0.1)",
      },
    },
  },
  plugins: [],
};

export default config;
