import type { StatTileSpec } from '~/Components/Recipe/StatTiles.vue'

/** Maps each difficulty code to its dot color + resource-string suffix for the label. */
const LEVELS: Record<string, { dotColor: string; labelKey: string }> = {
  easy: { dotColor: 'green', labelKey: 'DifficultyEasy' },
  medium: { dotColor: 'yellow', labelKey: 'DifficultyMedium' },
  hard: { dotColor: 'red', labelKey: 'DifficultyHard' },
}

/**
 * Build the difficulty stat tile, or `null` when difficulty is unset/unknown
 * (caller omits the tile). `t` resolves a VariantDetail.* resource-string suffix.
 */
export function difficultyTile(difficulty: string | undefined | null, t: (key: string) => string): StatTileSpec | null {
  if (!difficulty) return null
  const level = LEVELS[difficulty]
  if (!level) return null
  return {
    dotColor: level.dotColor,
    value: t(level.labelKey),
    label: t('Difficulty'),
  }
}
