import { renderSsr, expectNoRetiredMarkup } from '../../../support/ssr'
import { describe, expect, it } from 'vitest'
import '~/Utilities/StringExtensions'
import VariantSiblings from '~/Components/VariantDetail/VariantSiblings.vue'
import type { SiblingVariant } from '~/Types/Recipe'

const NAMES = ['Crispy Edge', 'Garlic Confit', 'Herb Ricotta', 'Lemon Butter', 'Nduja', 'Sage Brown', 'Smoked Chilli']

const sibling = (name: string, index: number): SiblingVariant => ({
  name,
  slug: `/recipes/gnocchi/${index}`,
  icon: 'fa-duotone fa-wheat',
  rating: index ? 4 : 0,
  totalTime: 20 + index * 5,
})

const render = (count = 3, over: Record<string, unknown> = {}) =>
  renderSsr(VariantSiblings, { variants: NAMES.slice(0, count).map(sibling), ...over })

const cardSlips = (html: string) => html.match(/<article class="kcc-slip kcc-slip--fill[^"]*"/g) ?? []

describe('VariantSiblings', () => {
  it('names the section on the rule, as a heading', async () => {
    const html = await render()

    expect(html).toMatch(/<div class="kcc-secname"><h2 class="kcc-section">\s*OtherVariants\s*<\/h2><\/div>/)
  })

  it('lays the siblings out as the same card grid the recipe page uses', async () => {
    const html = await render()

    expect(html).toContain('grid grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))] gap-x-7 gap-y-rule-1.5')
  })

  it('gives every sibling a slip of its own, cycling the tears so no two neighbours share one', async () => {
    const tears = [...(await render(7)).matchAll(/kcc-slip kcc-slip--fill kcc-recipe[^"]*kcc-tear-(\d)/g)].map(
      (match) => match[1],
    )

    expect(tears).toEqual(['1', '2', '3', '4', '5', '6', '1'])
  })

  it('starts the cycle two on when the sheet above ends on a 1, keeping each card’s turn', async () => {
    const tears = [
      ...(await render(7, { tearOffset: 2 })).matchAll(/kcc-slip kcc-slip--fill kcc-recipe[^"]*kcc-tear-(\d)/g),
    ].map((match) => match[1])

    expect(tears).toEqual(['3', '4', '5', '6', '1', '2', '3'])
  })

  it('links each card to the sibling and prints its name and time', async () => {
    const html = await render(2)

    expect(html).toContain('href="/recipes/gnocchi/0"')
    expect(html).toContain('<h3 class="kcc-title">Crispy Edge</h3>')
    // The variant page carries no minutes string, so the card prints the same literal its stat tiles do.
    expect(html).toContain('20 min')
  })

  it('turns every card over when asked, and none unasked', async () => {
    const flipped = cardSlips(await render(3, { flip: true }))
    expect(flipped).toHaveLength(3)
    flipped.forEach((slip) => expect(slip).toMatch(/ kcc-slip--flip kcc-tear-\d"$/))

    const unflipped = cardSlips(await render(3))
    expect(unflipped).toHaveLength(3)
    unflipped.forEach((slip) => expect(slip).not.toContain('kcc-slip--flip'))
  })

  it('renders nothing at all for a variant with no siblings', async () => {
    expect(await renderSsr(VariantSiblings, { variants: [] })).not.toContain('kcc-secname')
  })

  it('carries no Softbound remnant: no drawn ink, no pill paper, no coloured star', async () => {
    const html = await render()

    expectNoRetiredMarkup(html)
    expect(html).not.toContain('transition-shadow')
    expect(html).not.toContain('bg-paper-2')
  })
})
