import { renderSsr } from '../../../support/renderSsr'
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

const render = (count = 3) => renderSsr(VariantSiblings, { variants: NAMES.slice(0, count).map(sibling) })

describe('VariantSiblings', () => {
  it('names the section on the rule, as a heading', async () => {
    const html = await render()

    expect(html).toMatch(/<div class="kcc-secname"><h2>\s*OtherVariants\s*<\/h2><\/div>/)
  })

  it('lays the siblings out as the same card grid the recipe page uses', async () => {
    const html = await render()

    expect(html).toContain('grid grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))] gap-x-7 gap-y-9')
  })

  it('gives every sibling a slip of its own, cycling the tears so no two neighbours share one', async () => {
    const tears = [...(await render(7)).matchAll(/kcc-slip kcc-recipe[^"]*kcc-tear-(\d)/g)].map((match) => match[1])

    expect(tears).toEqual(['1', '2', '3', '4', '5', '6', '1'])
  })

  it('links each card to the sibling and prints its name and time', async () => {
    const html = await render(2)

    expect(html).toContain('href="/recipes/gnocchi/0"')
    expect(html).toContain('<h3 class="kcc-h4">Crispy Edge</h3>')
    // No page provides strings here, so the resolver hands back the bare key: the unit reads "Min".
    expect(html).toContain('20 Min')
  })

  it('renders nothing at all for a variant with no siblings', async () => {
    expect(await renderSsr(VariantSiblings, { variants: [] })).not.toContain('kcc-secname')
  })

  it('carries no Softbound remnant: no drawn ink, no pill paper, no coloured star', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/\bv-ink\b/)
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
    expect(html).not.toContain('text-rating-ink')
    expect(html).not.toContain('transition-shadow')
    expect(html).not.toContain('bg-paper-2')
  })
})
