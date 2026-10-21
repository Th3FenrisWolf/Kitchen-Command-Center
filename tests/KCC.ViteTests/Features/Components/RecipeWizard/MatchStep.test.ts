import { describe, expect, it } from 'vitest'
import MatchStep from '~/Components/RecipeWizard/MatchStep.vue'
import {
  buttonHolding,
  buttonWith,
  classesOf,
  enclosingTag,
  renderInWizard,
  unresolvedKeys,
} from '../../../support/recipeWizard'
import { expectNoRetiredMarkup, tagWith } from '../../../support/ssr'

const ENGLISH = {
  MatchTitle: 'Is this a new way to make {0}?',
  AddAsVariant: 'Add as a new variant of {0}',
  PossibleDuplicate: 'This looks almost the same as {0}, a variant of {1}.',
}

const STACK = { key: '33333333-4444-5555-6666-777777777777', name: 'Fluffy Buttermilk Pancakes' }
const LASAGNA = { key: '22222222-3333-4444-5555-666666666666', name: 'Legendary Lasagna' }
const TACOS = { key: '55555555-6666-7777-8888-999999999999', name: 'Weeknight Tacos' }

const MATCH = { recipeKey: STACK.key, recipeName: STACK.name, reason: 'Same dish with buttermilk.' }

const DUPLICATE = {
  variantKey: '44444444-5555-6666-7777-888888888888',
  variantName: 'Classic Stack',
  recipeName: STACK.name,
}

const render = (props: Record<string, unknown> = {}) =>
  renderInWizard(MatchStep, { match: MATCH, recipes: [LASAGNA, STACK, TACOS], canGoBack: true, tear: 2, ...props }, ENGLISH)

const openingTag = (html: string, element: string, needle = '') =>
  html.match(new RegExp(`<${element}\\b[^>]*${needle}[^>]*>`))?.[0] ?? ''

const options = (html: string) =>
  [...html.matchAll(/<option[^>]*value="([^"]+)"[^>]*>([^<]*)<\/option>/g)].map(([, value, name]) => [value, name!.trim()])

describe('MatchStep', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'data-testid="match-step"')).toContain('kcc-slip kcc-slip--wide kcc-tear-2')
  })

  it('hooks the match step on a crisp sheet, headed with the proposed recipe', async () => {
    const html = await render()
    const slip = tagWith(html, 'data-testid="match-step"')

    expect(classesOf(slip)).toEqual(['kcc-slip', 'kcc-slip--wide', 'kcc-tear-2'])
    expect(slip).toContain('--r:0')
    expect(html).toMatch(/<h2 class="kcc-heading">\s*Is this a new way to make Fluffy Buttermilk Pancakes\?\s*<\/h2>/)
  })

  it("gives the reader's reason for the proposal in soft body text, as a mark's reason", async () => {
    const reason = classesOf(tagWith(await render(), '>Same dish with buttermilk\\.'))

    expect(reason).toEqual(expect.arrayContaining(['kcc-body', 'text-ink-soft']))
    expect(reason).not.toContain('kcc-hand')
  })

  it('adds the photos as a new variant of the proposed recipe from a marker button', async () => {
    const html = await render()
    const accept = tagWith(html, 'data-testid="match-accept"')

    expect(accept).toMatch(/^<button /)
    expect(accept).toContain('type="button"')
    expect(classesOf(accept)).toContain('kcc-btn')
    expect(classesOf(accept)).not.toContain('kcc-btn--ghost')
    expect(buttonWith(html, 'data-testid="match-accept"')).toContain('Add as a new variant of Fluffy Buttermilk Pancakes')
  })

  it('wraps a long Add as a new variant onto 24px lines inside a pill two rules tall on one line', async () => {
    const accept = classesOf(tagWith(await render(), 'data-testid="match-accept"'))

    expect(accept).toEqual(
      expect.arrayContaining(['whitespace-normal', 'h-auto', '[line-height:var(--bl)]', 'py-rule-0.5', 'text-left']),
    )
    expect(accept).not.toContain('my-1')
  })

  it('takes Add as a new variant to a whole number of rules at any line count, so the list label lands on one', async () => {
    const html = await render()
    const accept = classesOf(tagWith(html, 'data-testid="match-accept"'))

    expect(accept).toContain('py-rule-0.5')
    expect(accept).toContain('[--h:calc(var(--bl)*2)]')
    expect(classesOf(enclosingTag(html, 'data-testid="match-accept"'))).toEqual(
      expect.arrayContaining(['flex-col', 'gap-y-rule-1']),
    )
  })

  it('wraps the recipe list and Add to another recipe onto the next rule, never 12px under', async () => {
    const row = classesOf(enclosingTag(await render(), 'data-testid="match-pick"', 1))

    expect(row).toEqual(expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-rule-1']))
  })

  it('lines Add to another recipe up with the list, in a row of two rules under the label', async () => {
    const html = await render()

    expect(classesOf(enclosingTag(html, 'data-testid="match-pick"'))).toEqual(['kcc-row'])
    expect(classesOf(enclosingTag(html, 'data-testid="match-pick"', 1))).toContain('items-end')
  })

  it('picks another published recipe from a labelled list that leaves out the proposed one', async () => {
    const html = await render()
    const select = openingTag(html, 'select')
    const id = select.match(/\sid="([^"]+)"/)?.[1]
    const pick = tagWith(html, 'data-testid="match-pick"')

    expect(id).toBeTruthy()
    expect(classesOf(openingTag(html, 'label', `for="${id}"`))).toContain('kcc-lbl')
    expect(html).toMatch(new RegExp(`<label[^>]*for="${id}"[^>]*>[\\s\\S]*?«PickRecipeLabel»[\\s\\S]*?</label>`))
    expect(options(html)).toEqual([
      [LASAGNA.key, LASAGNA.name],
      [TACOS.key, TACOS.name],
    ])
    expect(pick).toMatch(/^<button /)
    expect(classesOf(pick)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']))
    expect(buttonWith(html, 'data-testid="match-pick"')).toContain('«PickAnotherRecipe»')
  })

  it('starts the list on the recipe the member picked before', async () => {
    const html = await render({ chosenKey: TACOS.key })

    expect(openingTag(html, 'option', `value="${TACOS.key}"`)).toContain('selected')
    expect(openingTag(html, 'option', `value="${LASAGNA.key}"`)).not.toContain('selected')
  })

  it('offers no other recipe when the proposed one is the only one', async () => {
    const html = await render({ recipes: [STACK] })

    expect(html).not.toContain('<select')
    expect(html).not.toContain('match-pick')
  })

  it('keeps a new recipe from a ghost button', async () => {
    const html = await render()
    const keep = tagWith(html, 'data-testid="match-keep"')

    expect(keep).toMatch(/^<button /)
    expect(classesOf(keep)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']))
    expect(buttonWith(html, 'data-testid="match-keep"')).toContain('«KeepNewRecipe»')
  })

  it('names a possible duplicate and its recipe in a warning well', async () => {
    const html = await render({ possibleDuplicate: DUPLICATE })
    const notice = tagWith(html, 'data-testid="possible-duplicate"')

    expect(notice).toMatch(/^<p /)
    expect(classesOf(notice)).toEqual(expect.arrayContaining(['kcc-well', 'kcc-well--warning', 'kcc-kick']))
    expect(html).toMatch(
      /data-testid="possible-duplicate"[^>]*>\s*This looks almost the same as Classic Stack, a variant of Fluffy Buttermilk Pancakes\.\s*</,
    )
  })

  it('shows no possible duplicate when the read names none', async () => {
    expect(await render()).not.toContain('possible-duplicate')
  })

  it('goes back from a ghost button', async () => {
    const back = buttonHolding(await render(), '«Back»')

    expect(classesOf(back)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']))
  })

  it('sets Keep it as a new recipe and the Back row in whole rules, so whatever follows starts on one', async () => {
    const html = await render()

    expect(classesOf(enclosingTag(html, 'data-testid="match-keep"'))).toEqual(['kcc-row'])
    expect(classesOf(enclosingTag(html, 'fa-arrow-left', 1))).toEqual(['kcc-row', 'justify-between'])
  })

  it('offers no Back when the step has nowhere to go back to', async () => {
    expect(await render({ canGoBack: false })).not.toContain('«Back»')
  })

  it('resolves every string under the RecipeWizard group, and uses no retired markup', async () => {
    const html = await render({ possibleDuplicate: DUPLICATE })

    expect(unresolvedKeys(html)).toEqual([])
    expectNoRetiredMarkup(html)
  })
})
