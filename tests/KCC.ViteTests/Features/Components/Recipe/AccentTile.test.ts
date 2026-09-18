import { describe, expect, it } from 'vitest'
import { renderSsr } from '../../../support/renderSsr'
import AccentTile from '~/Components/Recipe/AccentTile.vue'
import { tileTearFor, washFor } from '~/Utilities/BrandColor'

const seed = 'Brown Butter Gnocchi'

describe('AccentTile', () => {
  it('renders the image with no radius when given one', async () => {
    const html = await renderSsr(AccentTile, { seed, image: '/img.jpg' })
    expect(html).toContain('class="block object-cover"')
    expect(html).not.toMatch(/rounded/)
  })

  it('falls back to a torn wax tile keyed off the seed', async () => {
    const html = await renderSsr(AccentTile, { seed, icon: 'fa-duotone fa-wheat' })
    // Vue's compiler merges a static `class` with a `:class` binding on the same element by appending the
    // static string after the dynamic list, not by source order — this is what it actually emits.
    expect(html).toBe(
      `<div class="kcc-torn"><div class="kcc-tear-tile-${tileTearFor(seed)} kcc-tile" style="--c:var(--color-${washFor(seed)});" aria-hidden="true"><i class="fa-duotone fa-wheat"></i></div></div>`,
    )
  })

  it('adds kcc-tile--lg when large', async () => {
    const html = await renderSsr(AccentTile, { seed, large: true })
    expect(html).toContain(`class="kcc-tear-tile-${tileTearFor(seed)} kcc-tile--lg kcc-tile"`)
  })
})
