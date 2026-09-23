import { describe, expect, it } from 'vitest'
import { siblingToCard } from '~/Components/Recipe/recipeCardModel'
import type { SiblingVariant } from '~/Types/Recipe'

const sibling = (over: Partial<SiblingVariant> = {}): SiblingVariant => ({
  name: 'Crispy Edge',
  slug: '/recipes/gnocchi/crispy-edge',
  icon: 'fa-duotone fa-wheat',
  rating: 0,
  totalTime: 35,
  ...over,
})

describe('siblingToCard', () => {
  it('carries the sibling to its own page, coloured by its own name', () => {
    const card = siblingToCard(sibling())

    expect(card.href).toBe('/recipes/gnocchi/crispy-edge')
    expect(card.name).toBe('Crispy Edge')
    expect(card.seed).toBe('Crispy Edge')
    expect(card.icon).toBe('fa-duotone fa-wheat')
  })

  it('prints the time on the meta line, in the minutes the page itself prints', () => {
    expect(siblingToCard(sibling()).meta).toEqual([{ key: 'time', icon: 'fa-solid fa-clock', text: '35 min' }])
  })

  it('makes the time the corner stat while the variant is unrated', () => {
    expect(siblingToCard(sibling()).notch).toEqual({ stat: 'time', text: '35 min' })
  })

  it('raises the rating to the corner once there is one, with no review count beside it', () => {
    const card = siblingToCard(sibling({ rating: 4.5 }))

    expect(card.notch).toEqual({ stat: 'rating', text: '4.5' })
    // A sibling arrives without a review count, so the card prints no "· N" and no empty-rating note.
    expect(card.rating).toBeUndefined()
  })

  it('leaves the sibling without badges, a description or a trailing stat', () => {
    const card = siblingToCard(sibling())

    expect(card.tags).toEqual([])
    expect(card.description).toBeUndefined()
    expect(card.trailingStat).toBeUndefined()
    expect(card.dataAttrs).toBeUndefined()
  })
})
