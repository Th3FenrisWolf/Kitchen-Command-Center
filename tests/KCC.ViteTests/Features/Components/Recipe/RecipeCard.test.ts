import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeCard from '~/Components/Recipe/RecipeCard.vue'
import { hitToCard } from '~/Components/Recipe/recipeCardModel'
import type { RecipeSearchHit } from '~/Types/Recipe'

const hit = (over: Partial<RecipeSearchHit> = {}): RecipeSearchHit => ({
  name: 'Weeknight Chicken Piccata',
  slug: '/recipes/chicken-piccata',
  icon: 'fa-solid fa-drumstick-bite',
  category: 'Mains',
  startedBy: 'Dana Whitfield',
  tags: ['Gluten-Free'],
  averageRating: 4.8,
  reviewCount: 12,
  variantCount: 5,
  fastestTime: 30,
  ...over,
})

// Matches what useResourceStrings hands the card when a key has no value: the key itself.
const rs = (key: string) => key

const countStars = (html: string) => (html.match(/fa-star\b/g) ?? []).length

// Renders through the search page's own mapper, so these cover the RecipeSearchHit -> card
// hop as well as the markup the shared card produces from it.
const render = (recipe: RecipeSearchHit) => renderToString(createSSRApp(RecipeCard, { card: hitToCard(recipe, rs) }))

describe('RecipeCard rating', () => {
  it('shows a star and the numeric rating when the recipe has reviews', async () => {
    const html = await render(hit())
    expect(countStars(html)).toBe(1)
    expect(html).toContain('4.8')
  })

  it('shows no star (no fake number) when the recipe has no reviews', async () => {
    const html = await render(hit({ averageRating: null, reviewCount: 0 }))
    expect(countStars(html)).toBe(0)
    expect(html).not.toContain('4.8')
  })

  it('renders the recipe name and tags', async () => {
    const html = await render(hit())
    expect(html).toContain('Weeknight Chicken Piccata')
    expect(html).toContain('Gluten-Free')
  })
})
