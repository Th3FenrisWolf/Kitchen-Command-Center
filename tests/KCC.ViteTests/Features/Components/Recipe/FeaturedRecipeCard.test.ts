import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import FeaturedRecipeCard from '~/Components/Recipe/FeaturedRecipeCard.vue'
import { hitToFeatured } from '~/Components/Recipe/recipeCardModel'
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

// Matches what useResourceStrings hands the card when a key has no value: the key itself.
const rs = (key: string) => key

describe('FeaturedRecipeCard from a search hit', () => {
  it('renders the recipe name and its numeric rating', async () => {
    const html = await renderToString(createSSRApp(FeaturedRecipeCard, { card: hitToFeatured(recipe, rs) }))
    expect(html).toContain('Sourdough Focaccia')
    expect(html).toContain('4.9')
    expect(html).toContain(recipe.slug)
  })
})
