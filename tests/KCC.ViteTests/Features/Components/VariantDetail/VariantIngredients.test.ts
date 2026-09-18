import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import VariantIngredients from '~/Components/VariantDetail/VariantIngredients.vue'
import type { Ingredient } from '~/Types/Recipe'

const ingredients: Ingredient[] = [
  { name: 'Plain flour', quantity: 2, unit: 'cups', isEyeballed: false },
  { name: 'Sea salt', unit: '', isEyeballed: true },
]

const render = (over: Record<string, unknown> = {}) =>
  renderSsr(VariantIngredients, { ingredients, baseServings: 4, ...over })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

const rowFor = (html: string, name: string) =>
  html.match(new RegExp(`<li[^>]*>(?:(?!</li>).)*${name}.*?</li>`, 's'))?.[0] ?? ''

describe('VariantIngredients sheet', () => {
  it('is a teal-washed sheet on the first tear, labelled with the basket', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-tear-1')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-basket-shopping" aria-hidden="true"></i>')
    expect(html).toMatch(/kcc-label[^>]*>.*?Ingredients/s)
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('pools its one wash low and left, clear of the list', async () => {
    const html = await render()
    const wash = tagWith(html, 'kcc-wash')

    expect(wash).toContain('--c:var(--color-teal)')
    expect(wash).toContain('--x:15%')
    expect(wash).toContain('--y:95%')
    expect(wash).toContain('--w:60%')
    expect(wash).toContain('--h:45%')
    expect((html.match(/kcc-wash/g) ?? []).length).toBe(1)
  })

  it('keeps the section heading for assistive tech while the label carries the printed name', async () => {
    const html = await render()

    expect(html).toMatch(/<h2 class="sr-only">\s*Ingredients\s*<\/h2>/)
    expect((html.match(/kcc-label/g) ?? []).length).toBe(1)
  })

  it('kicks the list off with what to do, and prints no second serving count', async () => {
    const html = await render()
    const kick = html.match(/<p class="kcc-kick">.*?<\/p>/s)?.[0] ?? ''

    expect(kick).toContain('tick what you have')
    // The serving count lives in the stepper's field; a second copy in the kick would drift from it.
    expect(kick).not.toContain('kcc-num')
  })

  it('prints the ingredients as a checklist on the rule: box, name, quantity in Sono', async () => {
    const html = await render()

    expect(html).toMatch(/<ul class="kcc-check[^"]*">/)
    const flour = rowFor(html, 'Plain flour')
    expect(flour).toMatch(
      /^<li[^>]*><input id="([^"]+)" type="checkbox" class="sr-only"><label for="\1" class="kcc-box"><\/label><label for="\1">[^<]*Plain flour[^<]*<\/label><span class="kcc-q">2 cups<\/span><\/li>$/,
    )
  })

  it('leaves every row unticked on the server; ticking is client state', async () => {
    const html = await render()

    expect(html).not.toContain('kcc-done')
    expect(html).not.toContain('kcc-box--on')
  })

  it('reads an eyeballed ingredient as a quantity of "to taste"', async () => {
    const salt = rowFor(await render(), 'Sea salt')

    expect(salt).toContain('<span class="kcc-q">ToTaste</span>')
  })

  it('scales through the kit stepper, never below one serving', async () => {
    const html = await render()

    expect(html).toContain('aria-label="Makes"')
    expect(html).toContain('aria-label="Decrease Makes"')
    expect(html).toContain('aria-label="Increase Makes"')
    expect(html).toContain('min="1"')
    expect(html).toContain('value="4"')
    expect(html).toMatch(/class="kcc-lbl">\s*Makes\s*</)
  })

  it('drops the scaler when the stored amounts are not per-serving', async () => {
    const html = await render({ baseServings: 0 })

    expect(html).not.toContain('aria-label="Decrease Makes"')
    expect(html).toContain('kcc-check')
  })

  it('carries no Softbound hook, weight or paper radius', async () => {
    const html = await render()

    expect(html).not.toContain('sk-')
    expect(html).not.toContain('font-bold')
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
  })
})
