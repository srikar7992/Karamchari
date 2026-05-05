/**
 * Design System Color Tokens.
 * Uses HSL for programmatic manipulation and harmonious palettes.
 */
export const colors = {
  // Brand
  primary: {
    DEFAULT: "hsl(221, 83%, 53%)", // Vibrant Royal Blue
    light: "hsl(221, 83%, 63%)",
    dark: "hsl(221, 83%, 43%)",
    transparent: "hsla(221, 83%, 53%, 0.1)",
  },

  // Status
  success: {
    DEFAULT: "hsl(142, 71%, 45%)", // Emerald
    bg: "hsla(142, 71%, 45%, 0.1)",
    text: "hsl(142, 71%, 25%)",
  },
  warning: {
    DEFAULT: "hsl(38, 92%, 50%)", // Amber
    bg: "hsla(38, 92%, 50%, 0.1)",
    text: "hsl(38, 92%, 20%)",
  },
  danger: {
    DEFAULT: "hsl(0, 84%, 60%)", // Rose Red
    bg: "hsla(0, 84%, 60%, 0.1)",
    text: "hsl(0, 84%, 30%)",
  },
  info: {
    DEFAULT: "hsl(199, 89%, 48%)", // Sky Blue
    bg: "hsla(199, 89%, 48%, 0.1)",
    text: "hsl(199, 89%, 28%)",
  },

  // Neutrals (Dark Mode oriented)
  background: {
    DEFAULT: "hsl(222, 47%, 11%)", // Deep Space Blue
    surface: "hsl(222, 47%, 15%)",
    border: "hsl(217, 32%, 20%)",
  },

  text: {
    primary: "hsl(210, 40%, 98%)",
    secondary: "hsl(215, 20%, 65%)",
    muted: "hsl(215, 16%, 47%)",
  },
} as const;
