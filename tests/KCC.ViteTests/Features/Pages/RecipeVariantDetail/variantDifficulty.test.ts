import { describe, expect, it } from 'vitest'
import { difficultyTile } from '~/Components/VariantDetail/variantDifficulty'

const t = (key: string) => `T:${key}`

describe('difficultyTile', () => {
  it('returns null when difficulty is unset', () => {
    expect(difficultyTile(undefined, t)).toBeNull()
    expect(difficultyTile('', t)).toBeNull()
  })

  it('maps easy/medium/hard to a label and dot color', () => {
    expect(difficultyTile('easy', t)).toEqual({
      dotColor: 'green',
      value: 'T:DifficultyEasy',
      label: 'T:Difficulty',
    })
    expect(difficultyTile('medium', t)).toEqual({
      dotColor: 'yellow',
      value: 'T:DifficultyMedium',
      label: 'T:Difficulty',
    })
    expect(difficultyTile('hard', t)).toEqual({
      dotColor: 'red',
      value: 'T:DifficultyHard',
      label: 'T:Difficulty',
    })
  })

  it('returns null for an unrecognized code', () => {
    expect(difficultyTile('extreme', t)).toBeNull()
  })
})
