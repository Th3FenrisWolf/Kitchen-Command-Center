import { describe, expect, it } from 'vitest'
import BasicsStep from '~/Components/RecipeWizard/BasicsStep.vue'
import { emptyValues, type WizardValues } from '~/Components/RecipeWizard/wizardValues'
import { classesOf, describedBy, enclosingTag, markKinds, readerMark, renderInWizard } from '../../../support/recipeWizard'
import { tagWith } from '../../../support/ssr'

const READ: WizardValues = {
  ...emptyValues(),
  recipeName: 'Skillet Cornbread',
  recipeDescription: 'Golden and crumbly.',
  marks: {
    'recipe.name': readerMark('unclear', 'Smudged'),
    'recipe.description': readerMark('written', 'Not in the photos'),
  },
}

const render = (values: WizardValues) => renderInWizard(BasicsStep, { values })

describe('BasicsStep', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render(READ)

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-tear-1')
  })

  it('shows the mark a read left on the recipe name and on its description, with its reason', async () => {
    const html = await render(READ)

    expect(markKinds(html).sort()).toEqual(['unclear', 'written'])
    expect(html).toContain('Smudged')
    expect(html).toContain('Not in the photos')
  })

  it('describes each marked field by its own mark', async () => {
    const html = await render(READ)

    expect(describedBy(html, 'data-testid="wizard-recipe-name"')).toContain('data-kind="unclear"')
    expect(describedBy(html, 'data-testid="wizard-recipe-description"')).toContain('data-kind="written"')
  })

  it('sets each mark on the rule under its field, outside the box that centres the control', async () => {
    const html = await render(READ)

    for (const control of ['data-testid="wizard-recipe-name"', 'data-testid="wizard-recipe-description"']) {
      const markId = tagWith(html, control).match(/\saria-describedby="([^"]+)"/)?.[1]
      expect(classesOf(enclosingTag(html, `\\sid="${markId}"`))).toEqual(['flex', 'flex-col'])
    }
  })

  it('shows no mark on values the member typed', async () => {
    const html = await render({ ...emptyValues(), recipeName: 'Toast', recipeDescription: 'Buttered.' })

    expect(markKinds(html)).toEqual([])
    expect(tagWith(html, 'data-testid="wizard-recipe-name"')).not.toContain('aria-describedby')
  })
})
