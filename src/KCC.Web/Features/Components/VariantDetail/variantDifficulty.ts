import type { StatTileSpec } from '~/Components/Recipe/StatTiles.vue'

const LEVELS: Record<string, { dotColor: string; labelKey: string }> = {
  easy: { dotColor: 'green', labelKey: 'DifficultyEasy' },
  medium: { dotColor: 'yellow', labelKey: 'DifficultyMedium' },
  hard: { dotColor: 'red', labelKey: 'DifficultyHard' },
}

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
