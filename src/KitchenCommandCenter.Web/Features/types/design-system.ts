/**
 * Design System Types
 *
 * These types are based on the custom colors defined in src/styles/tailwind-config.css
 * and provide type safety for Tailwind CSS classes throughout the application.
 */

// Base color names from your design system
export type ColorName =
  // Core colors
  | 'base'
  | 'bone'
  | 'onyx'
  // Palette colors
  | 'red'
  | 'maroon'
  | 'peach'
  | 'yellow'
  | 'green'
  | 'teal'
  | 'sky'
  | 'sapphire'
  | 'blue'
  | 'lavender'
  | 'pink'
  | 'mauve'
  | 'rosewater'
  | 'flamingo'

// Utility types for Tailwind classes
export type BackgroundColor = `bg-${ColorName}`
export type TextColor = `text-${ColorName}`
export type BorderColor = `border-${ColorName}`

// Common color combinations for components
export interface ColorTheme {
  background: BackgroundColor
  text: TextColor
  accent?: BackgroundColor
  accentText?: TextColor
}
