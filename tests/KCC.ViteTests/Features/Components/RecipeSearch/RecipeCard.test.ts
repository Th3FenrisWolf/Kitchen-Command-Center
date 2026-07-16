import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeCard from '~/Components/RecipeSearch/RecipeCard.vue'
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

const countStars = (html: string) => (html.match(/fa-star\b/g) ?? []).length
const render = (recipe: RecipeSearchHit) => renderToString(createSSRApp(RecipeCard, { recipe }))

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
