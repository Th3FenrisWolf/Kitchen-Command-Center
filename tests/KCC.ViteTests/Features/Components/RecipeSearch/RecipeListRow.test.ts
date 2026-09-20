import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import RecipeListRow from '~/Components/RecipeSearch/RecipeListRow.vue'
import { sheetTearFor } from '~/Utilities/BrandColor'
import type { RecipeSearchHit } from '~/Types/Recipe'

const NAME = 'Brown Butter Gnocchi'

const hit: RecipeSearchHit = {
  name: NAME,
  slug: '/recipes/brown-butter-gnocchi',
  icon: 'fa-solid fa-wheat',
  category: 'Pasta',
  startedBy: 'Dana Whitfield',
  tags: ['Vegetarian'],
  averageRating: 4.5,
  reviewCount: 28,
  variantCount: 6,
  fastestTime: 25,
}

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('RecipeListRow', () => {
  it('hands the localized hit to the shared torn row, hooks intact', async () => {
    const html = await renderSsr(RecipeListRow, { recipe: hit })

    const anchor = tagWith(html, 'kcc-slip')
    expect(anchor).toContain('data-testid="recipe-card"')
    expect(anchor).toContain(`data-recipe-name="${NAME}"`)
    expect(anchor).toContain(`kcc-tear-${sheetTearFor(NAME)}`)
    expect(html).toContain(`<h3 class="kcc-h4">${NAME}</h3>`)
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  // A hash of the name can give two neighbours the same tear; the list view hands each row its cycle index.
  it('takes the tear the list hands it over its own hash', async () => {
    const html = await renderSsr(RecipeListRow, { recipe: hit, tear: 4 })

    expect(tagWith(html, 'kcc-slip')).toContain('kcc-tear-4')
  })
})
