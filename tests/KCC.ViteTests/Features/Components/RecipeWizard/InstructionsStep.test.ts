import { describe, expect, it } from 'vitest'
import InstructionsStep from '~/Components/RecipeWizard/InstructionsStep.vue'
import { emptyValues } from '~/Components/RecipeWizard/wizardValues'
import { describedBy, markKinds, readerMark, renderInWizard } from '../../../support/recipeWizard'

const render = () =>
  renderInWizard(InstructionsStep, {
    values: {
      ...emptyValues(),
      instructions: [{ text: 'Heat the skillet.' }, { text: 'Bake.', mark: readerMark('unclear', 'Time cut off') }],
    },
  })

const rows = (html: string) => html.match(/<li\b[\s\S]*?<\/li>/g) ?? []

describe('InstructionsStep', () => {
  it("shows a step's mark on its own row", async () => {
    const [first = '', second = ''] = rows(await render())

    expect(markKinds(first)).toEqual([])
    expect(markKinds(second)).toEqual(['unclear'])
    expect(second).toContain('Time cut off')
  })

  it('describes a marked step by its mark', async () => {
    const [, second = ''] = rows(await render())

    expect(describedBy(second, 'data-testid="wizard-instruction-text"')).toContain('data-kind="unclear"')
  })
})
