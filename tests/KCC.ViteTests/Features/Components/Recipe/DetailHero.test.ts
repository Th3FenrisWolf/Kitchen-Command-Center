import { createSSRApp } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import DetailHero from '~/Components/Recipe/DetailHero.vue'

// Count star-family FontAwesome icons (fa-star, fa-star-half-stroke) in rendered markup.
// `fa-star\b` matches the leading token of each variant exactly once, so this equals the
// number of star glyphs actually painted — the whole point of the "6 stars" regression.
const countStars = (html: string) => (html.match(/fa-star\b/g) ?? []).length

const render = (props: Record<string, unknown>) =>
  renderToString(createSSRApp(DetailHero, { title: 'T', seed: 's', description: 'd', ...props }))

describe('DetailHero rating', () => {
  it('renders exactly five stars when a rating exists (no stray decorative star)', async () => {
    const html = await render({ averageRating: 4.5, reviewCount: 5 })
    expect(countStars(html)).toBe(5)
  })

  // An unreviewed recipe reads as text only, matching the empty state on the recipe cards.
  // A zero count and a missing one are the same state, so both inputs must stay starless.
  it.each([
    ['no reviews', { averageRating: 0, reviewCount: 0 }],
    ['no rating data at all', {}],
  ])('renders no stars with %s', async (_label, props) => {
    const html = await render(props)
    expect(countStars(html)).toBe(0)
  })
})
