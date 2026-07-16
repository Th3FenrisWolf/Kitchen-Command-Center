import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeSpotlight from '~/Components/RecipeSearch/RecipeSpotlight.vue'
import type { RecipeSearchHit } from '~/Types/Recipe'

const recipe: RecipeSearchHit = {
  name: 'Sourdough Focaccia',
  slug: '/recipes/focaccia',
  icon: 'fa-solid fa-bread-slice',
  category: 'Breads',
  startedBy: 'Marcus Hale',
  tags: ['Vegan'],
  averageRating: 4.9,
  reviewCount: 20,
  variantCount: 6,
  fastestTime: 45,
}

describe('RecipeSpotlight', () => {
  it('renders the recipe name and its numeric rating', async () => {
    const html = await renderToString(createSSRApp(RecipeSpotlight, { recipe }))
    expect(html).toContain('Sourdough Focaccia')
    expect(html).toContain('4.9')
    expect(html).toContain(recipe.slug)
  })
})
