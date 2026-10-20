import { describe, expect, it } from 'vitest'
import InstructionsStep from '~/Components/RecipeWizard/InstructionsStep.vue'
import { emptyValues } from '~/Components/RecipeWizard/wizardValues'
import { classesOf, describedBy, enclosingTag, markKinds, readerMark, renderInWizard } from '../../../support/recipeWizard'
import { tagWith } from '../../../support/ssr'

const render = () =>
  renderInWizard(InstructionsStep, {
    values: {
      ...emptyValues(),
      instructions: [{ text: 'Heat the skillet.' }, { text: 'Bake.', mark: readerMark('unclear', 'Time cut off') }],
    },
  })

const rows = (html: string) => html.match(/<li\b[\s\S]*?<\/li>/g) ?? []

describe('InstructionsStep', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-tear-1')
  })

  it('sets Add step in a row of two rules, so the step navigation under it starts on a rule', async () => {
    expect(classesOf(enclosingTag(await render(), 'fa-plus', 1))).toEqual(['kcc-row'])
  })

  it("shows a step's mark on its own row", async () => {
    const [first = '', second = ''] = rows(await render())

    expect(markKinds(first)).toEqual([])
    expect(markKinds(second)).toEqual(['unclear'])
    expect(second).toContain('Time cut off')
  })

  it("sets a step's mark straight under its text box, with no row gap to push it off the rule", async () => {
    const [, second = ''] = rows(await render())
    const row = classesOf(second.match(/^<li\b[^>]*>/)?.[0] ?? '')

    expect(row).toContain('gap-y-0')
    expect(classesOf(tagWith(second, 'data-testid="value-mark"'))).toContain('col-start-2')
  })

  it('describes a marked step by its mark', async () => {
    const [, second = ''] = rows(await render())

    expect(describedBy(second, 'data-testid="wizard-instruction-text"')).toContain('data-kind="unclear"')
  })
})
