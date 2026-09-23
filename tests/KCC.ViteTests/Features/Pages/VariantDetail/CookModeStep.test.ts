import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import CookModeStep from '~/Pages/VariantDetail/CookModeStep.vue'
import type { Ingredient, Instruction } from '~/Types/Recipe'

// CookMode itself teleports to <body> behind an `isMounted` gate, so the server prints a comment
// placeholder and its dialog shell can only be asserted by the e2e suite. The step is a plain
// component, so everything printed inside the overlay is covered here.
const instruction: Instruction = { step: 1, text: 'Simmer the sauce for 10-12 minutes, stirring often.' }

const ingredients: Ingredient[] = [
  { name: 'Plain flour', quantity: 2, unit: 'cups', isEyeballed: false },
  { name: 'Sea salt', unit: '', isEyeballed: true },
]

const render = (over: Record<string, unknown> = {}) =>
  renderSsr(CookModeStep, { instruction, ingredients, baseServings: 4, currentServings: 8, ...over })

const rowFor = (html: string, name: string) =>
  html.match(new RegExp(`<li[^>]*>(?:(?!</li>).)*${name}.*?</li>`, 's'))?.[0] ?? ''

describe('CookModeStep', () => {
  it('sets the instruction at display size, to be read from across the kitchen', async () => {
    const html = await render()

    expect(html).toMatch(/<p class="kcc-h3">\s*Simmer the sauce for 10-12 minutes, stirring often\.\s*<\/p>/)
  })

  it('drops the number disc: the header kick counts the steps', async () => {
    const html = await render()

    expect(html).not.toContain('rounded-full')
    expect(html).not.toContain('place-items-center')
  })

  it('keeps every duration in the step as a timer, in a row that wraps', async () => {
    const html = await render()

    expect(html).toContain('flex flex-wrap')
    expect((html.match(/role="timer"/g) ?? []).length).toBe(1)
    expect(html).toContain('data-test="timer-display"')
  })

  it('prints no timer row for a step with no duration in it', async () => {
    const html = await render({ instruction: { text: 'Season to taste.' } })

    expect(html).not.toContain('role="timer"')
  })

  it('kicks the ingredient list off with its name, and names the list by it', async () => {
    const html = await render()

    expect(html).toMatch(/<p id="[^"]+" class="kcc-kick">\s*Ingredients\s*<\/p>/)
    const kickId = html.match(/<p id="([^"]+)" class="kcc-kick">/)?.[1] ?? ''
    expect(html).toContain(`aria-labelledby="${kickId}"`)
    // The kick is the printed name; a heading level inside a dialog would be inventing structure.
    expect(html).not.toMatch(/<h[1-6][\s>]/)
  })

  it('prints the ingredients as a tickable checklist on the rule, keeping the cook-ingredient hook', async () => {
    const html = await render()

    expect(html).toMatch(/<ul class="kcc-check[^"]*"/)
    expect((html.match(/data-test="cook-ingredient"/g) ?? []).length).toBe(2)

    const flour = rowFor(html, 'Plain flour')
    expect(flour).toContain('data-test="cook-ingredient"')
    expect(flour).toMatch(
      /^<li[^>]*><input id="([^"]+)" type="checkbox" class="sr-only"><label for="\1" class="kcc-box"><\/label><label for="\1">[^<]*Plain flour[^<]*<\/label><span class="kcc-q">4 cups<\/span><\/li>$/,
    )
  })

  it('scales the amounts with the overlay servings', async () => {
    const html = await render({ currentServings: 4 })

    expect(rowFor(html, 'Plain flour')).toContain('<span class="kcc-q">2 cups</span>')
  })

  it('reads an eyeballed ingredient as a quantity of "to taste"', async () => {
    expect(rowFor(await render(), 'Sea salt')).toContain('<span class="kcc-q">ToTaste</span>')
  })

  it('leaves every row unticked on the server; ticking is client state', async () => {
    const html = await render()

    expect(html).not.toContain('kcc-done')
    expect(html).not.toContain('kcc-box--on')
  })

  it('carries no Softbound hook, weight, inset panel or paper radius', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/\bv-ink\b/)
    expect(html).not.toContain('font-bold')
    expect(html).not.toContain('<b>')
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
    expect(html).not.toContain('bg-paper-2')
    expect(html).not.toContain('border-rule')
  })
})
