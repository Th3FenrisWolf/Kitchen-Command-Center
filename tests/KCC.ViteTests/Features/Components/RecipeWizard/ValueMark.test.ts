import { describe, expect, it } from 'vitest'
import ValueMark from '~/Components/RecipeWizard/ValueMark.vue'
import type { Mark } from '~/Components/RecipeWizard/wizardValues'
import { classesOf, renderInWizard, unresolvedKeys } from '../../../support/recipeWizard'
import { expectNoRetiredMarkup, tagWith } from '../../../support/ssr'

const ENGLISH = {
  MarkReasonShortened: 'Shortened to fit.',
  MarkReasonOutOfRange: 'Outside the allowed range.',
  MarkReasonTranslated: 'Translated from {0}.',
  MarkReasonPrintedAs: 'Printed as {0}',
}

const mark = (over: Partial<Mark>): Mark => ({ kind: 'estimate', reason: null, code: null, arg: null, ...over })

const render = (value: Mark) => renderInWizard(ValueMark, { mark: value }, ENGLISH)

const badge = (html: string) => html.match(/<span class="kcc-badge[^"]*">([\s\S]*?)<\/span>/)?.[1]?.trim()

// The reason line nests at most one span, its argument, or the empty comment Vue leaves where it is absent.
const reasonLine = (html: string) => {
  const [, open = '', inner = ''] =
    html.match(/(<span class="kcc-body[^"]*">)((?:[^<]|<!---->|<span[^>]*>[^<]*<\/span>)*)<\/span>/) ?? []
  return { open, inner, text: inner.replace(/<[^>]*>/g, '').trim() }
}

describe('ValueMark', () => {
  it.each([
    ['estimate', '«MarkEstimate»'],
    ['unclear', '«MarkUnclear»'],
    ['written', '«MarkWritten»'],
    ['translated', '«MarkTranslated»'],
    ['suggested', '«MarkSuggested»'],
    ['calculated', '«MarkCalculated»'],
  ] as const)('names a mark of the %s kind in a badge, and carries its kind', async (kind, name) => {
    const html = await render(mark({ kind }))

    expect(tagWith(html, 'data-testid="value-mark"')).toContain(`data-kind="${kind}"`)
    expect(badge(html)).toBe(name)
  })

  it('gives the reason the reader wrote, as it wrote it, in body text', async () => {
    const html = await render(mark({ kind: 'unclear', reason: 'Printed as 2 to 3' }))

    expect(reasonLine(html).text).toBe('Printed as 2 to 3')
    expect(classesOf(reasonLine(html).open)).toEqual(expect.arrayContaining(['kcc-body']))
  })

  it('prints a reason from the reader as text, never as markup', async () => {
    const html = await render(mark({ kind: 'unclear', reason: '<b>2 & 3</b>' }))

    expect(html).toContain('&lt;b&gt;2 &amp; 3&lt;/b&gt;')
    expect(html).not.toContain('<b>')
  })

  it.each([
    ['shortened', null, 'Shortened to fit.'],
    ['outOfRange', null, 'Outside the allowed range.'],
    ['translated', 'French', 'Translated from French.'],
    ['printedAs', 'intermediate', 'Printed as intermediate'],
    ['printedAs', '1200 kJ', 'Printed as 1200 kJ'],
  ] as const)('renders the %s code from its MarkReason key, with its argument', async (code, arg, text) => {
    const kinds = { translated: 'translated', printedAs: 'suggested' } as const
    const html = await render(mark({ kind: kinds[code as 'translated' | 'printedAs'] ?? 'unclear', code, arg }))

    expect(reasonLine(html).text).toBe(text)
    expect(classesOf(reasonLine(html).open)).toEqual(expect.arrayContaining(['kcc-body']))
  })

  it.each(['1046 kJ', '1.5 g'])('sets a printed amount, %s, in Sono within the reason line', async (arg) => {
    const html = await render(mark({ kind: 'calculated', code: 'printedAs', arg }))

    expect(reasonLine(html).inner).toBe(`Printed as <span class="kcc-num">${arg}</span>`)
  })

  it('leaves a printed word in the body face', async () => {
    const html = await render(mark({ kind: 'suggested', code: 'printedAs', arg: 'intermediate' }))

    expect(reasonLine(html).inner).not.toContain('kcc-num')
  })

  it('prints an argument as text, never as markup', async () => {
    const html = await render(mark({ kind: 'calculated', code: 'printedAs', arg: '<b>1 & 2</b>' }))

    expect(html).toContain('&lt;b&gt;1 &amp; 2&lt;/b&gt;')
    expect(html).not.toContain('<b>')
  })

  it("falls back to the reader's reason when a printedAs code comes without its argument", async () => {
    const html = await render(mark({ kind: 'suggested', reason: 'Called plant-based', code: 'printedAs', arg: null }))

    expect(html).toContain('Called plant-based')
    expect(html).not.toContain('Printed as')
  })

  it('takes whole rules: a block row that stacks its lines with no gap, and a 24px badge the kit centres on the first rule', async () => {
    const html = await render(mark({ kind: 'unclear', reason: 'Printed as 2 to 3' }))
    const root = classesOf(tagWith(html, 'data-testid="value-mark"'))

    expect(root).toEqual(expect.arrayContaining(['flex', 'items-start', 'gap-x-2']))
    expect(root).not.toEqual(expect.arrayContaining(['inline-flex']))
    expect(root.filter((name) => /^gap-(y-)?\d/.test(name))).toEqual([])
    expect(classesOf(tagWith(html, 'class="kcc-badge'))).toEqual(['kcc-badge'])
  })

  it('shows the kind alone when the mark gives no reason', async () => {
    const html = await render(mark({ kind: 'written' }))

    expect(html).not.toContain('kcc-body')
  })

  it('resolves every string under the RecipeWizard group', async () => {
    expect(unresolvedKeys(await render(mark({ kind: 'translated', code: 'translated', arg: 'French' })))).toEqual([])
  })

  it('uses no retired markup', async () => {
    expectNoRetiredMarkup(await render(mark({ kind: 'unclear', reason: 'Smudged' })))
  })
})
