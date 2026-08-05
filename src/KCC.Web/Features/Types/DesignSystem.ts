export const BRAND_BACKGROUND_COLORS = [
  'bg-rosewater',
  'bg-flamingo',
  'bg-pink',
  'bg-mauve',
  'bg-red',
  'bg-maroon',
  'bg-peach',
  'bg-yellow',
  'bg-green',
  'bg-teal',
  'bg-sky',
  'bg-sapphire',
  'bg-blue',
  'bg-lavender',
] as const

export type BrandBackgroundColor = (typeof BRAND_BACKGROUND_COLORS)[number]

export const BACKGROUND_COLORS = [
  ...BRAND_BACKGROUND_COLORS,
  'bg-surface-200',
  'bg-surface-300',
  'bg-surface-400',
  'bg-surface-500',
  'bg-surface-600',
  'bg-surface-700',
  'bg-overlay-200',
  'bg-overlay-300',
  'bg-overlay-400',
  'bg-bone',
  'bg-bone-dark',
  'bg-onyx',
  'bg-onyx-light',
] as const

export type BackgroundColor = (typeof BACKGROUND_COLORS)[number]

export const BRAND_TEXT_COLORS = [
  'text-rosewater',
  'text-flamingo',
  'text-pink',
  'text-mauve',
  'text-red',
  'text-maroon',
  'text-peach',
  'text-yellow',
  'text-green',
  'text-teal',
  'text-sky',
  'text-sapphire',
  'text-blue',
  'text-lavender',
] as const

export type BrandTextColor = (typeof BRAND_TEXT_COLORS)[number]

export const TEXT_COLORS = [
  ...BRAND_TEXT_COLORS,
  'text-surface-200',
  'text-surface-300',
  'text-surface-400',
  'text-surface-500',
  'text-surface-600',
  'text-surface-700',
  'text-overlay-200',
  'text-overlay-300',
  'text-overlay-400',
  'text-bone',
  'text-bone-dark',
  'text-onyx',
  'text-onyx-light',
] as const

export type TextColor = (typeof TEXT_COLORS)[number]
