import { describe, expect, it } from 'vitest'
import ValueMark from '~/Components/RecipeWizard/ValueMark.vue'
import type { Mark } from '~/Components/RecipeWizard/wizardValues'
import { classesOf, renderInWizard, unresolvedKeys } from '../../../support/recipeWizard'
import { expectNoRetiredMarkup, tagWith } from '../../../support/ssr'

const ENGLISH = {
  MarkReasonShortened: 'Shortened to fit.',
  MarkReasonOutOfRange: 'Outside the allowed range.',
  MarkReasonTranslated: 'Translated from {0}.',
}

const mark = (over: Partial<Mark>): Mark => ({ kind: 'estimate', reason: null, code: null, arg: null, ...over })

const render = (value: Mark) => renderInWizard(ValueMark, { mark: value }, ENGLISH)

const badge = (html: string) => html.match(/<span class="kcc-badge[^"]*">([\s\S]*?)<\/span>/)?.[1]?.trim()

const literal = (text: string) => text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')

const reasonTag = (html: string, text: string) =>
  html.match(new RegExp(`<[a-z]+[^>]*>\\s*${literal(text)}\\s*</`))?.[0] ?? ''

describe('ValueMark', () => {
  it.each([
    ['estimate', '«MarkEstimate»'],
    ['unclear', '«MarkUnclear»'],
    ['written', '«MarkWritten»'],
    ['translated', '«MarkTranslated»'],
  ] as const)('names a mark of the %s kind in a badge, and carries its kind', async (kind, name) => {
    const html = await render(mark({ kind }))

    expect(tagWith(html, 'data-testid="value-mark"')).toContain(`data-kind="${kind}"`)
    expect(badge(html)).toBe(name)
  })

  it('gives the reason the reader wrote, as it wrote it, in body text', async () => {
    const html = await render(mark({ kind: 'unclear', reason: 'Printed as 2 to 3' }))

    expect(classesOf(reasonTag(html, 'Printed as 2 to 3'))).toEqual(expect.arrayContaining(['kcc-body']))
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
  ] as const)('renders the %s code from its MarkReason key, with its argument', async (code, arg, text) => {
    const html = await render(mark({ kind: code === 'translated' ? 'translated' : 'unclear', code, arg }))

    expect(classesOf(reasonTag(html, text))).toEqual(expect.arrayContaining(['kcc-body']))
  })

  it('takes whole rules: a block row that stacks its lines with no gap, and a 22px badge padded to the first rule', async () => {
    const html = await render(mark({ kind: 'unclear', reason: 'Printed as 2 to 3' }))
    const root = classesOf(tagWith(html, 'data-testid="value-mark"'))

    expect(root).toEqual(expect.arrayContaining(['flex', 'items-start', 'gap-x-2']))
    expect(root).not.toEqual(expect.arrayContaining(['inline-flex']))
    expect(root.filter((name) => /^gap-(y-)?\d/.test(name))).toEqual([])
    expect(classesOf(tagWith(html, 'class="kcc-badge'))).toEqual(['kcc-badge', 'my-px'])
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
