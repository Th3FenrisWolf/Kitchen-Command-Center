import { describe, expect, it } from 'vitest'
import VariantStep from '~/Components/RecipeWizard/VariantStep.vue'
import { emptyValues, type WizardValues } from '~/Components/RecipeWizard/wizardValues'
import { classesOf, describedBy, enclosingTag, markKinds, readerMark, renderInWizard } from '../../../support/recipeWizard'
import { tagWith } from '../../../support/ssr'

const READ: WizardValues = {
  ...emptyValues(),
  variantName: 'Honey Butter',
  variantDescription: 'Brushed with honey butter.',
  prepTime: 15,
  cookTime: 25,
  servings: 8,
  marks: {
    'variant.name': readerMark('written', 'Named for you'),
    'variant.description': readerMark('written', 'Not in the photos'),
    'variant.prepTime': readerMark('estimate', 'Not printed'),
    'variant.cookTime': readerMark('unclear', 'Printed as 20 to 25'),
    'variant.servings': { kind: 'translated', reason: null, code: 'translated', arg: 'Spanish' },
  },
}

const stepper = (name: string) => `\\sid="[^"]*-${name}"`

const CONTROLS = [
  'data-testid="wizard-variant-name"',
  'data-testid="wizard-variant-description"',
  stepper('prep-time'),
  stepper('cook-time'),
  stepper('servings'),
]

const PROPS = { mode: 'variant', values: READ }

const render = (values: WizardValues) => renderInWizard(VariantStep, { mode: 'variant', values })

describe('VariantStep', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render(READ)

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-tear-1')
  })

  it('shows the mark a read left on each value', async () => {
    const html = await render(READ)

    expect(markKinds(html).sort()).toEqual(['estimate', 'translated', 'unclear', 'written', 'written'])
    expect(html).toContain('Named for you')
    expect(html).toContain('Printed as 20 to 25')
  })

  it('describes each marked field by a mark of its own', async () => {
    const html = await render(READ)
    const marks = CONTROLS.map((control) => describedBy(html, control))

    expect(marks.map((mark) => mark.match(/data-kind="(\w+)"/)?.[1])).toEqual([
      'written',
      'written',
      'estimate',
      'unclear',
      'translated',
    ])
    expect(new Set(marks).size).toBe(CONTROLS.length)
  })

  it('sets each mark on the rule under its field, outside the box that centres the control', async () => {
    const html = await render(READ)

    for (const control of CONTROLS) {
      const markId = tagWith(html, control).match(/\saria-describedby="([^"]+)"/)?.[1]
      expect(classesOf(enclosingTag(html, `\\sid="${markId}"`))).toEqual(['flex', 'flex-col'])
    }
  })

  it('shows no mark on values the member typed', async () => {
    const html = await render({ ...emptyValues(), variantName: 'Classic', prepTime: 10 })

    expect(markKinds(html)).toEqual([])
    expect(tagWith(html, stepper('prep-time'))).toMatch(/^<input /)
    expect(tagWith(html, stepper('prep-time'))).not.toContain('aria-describedby')
  })
  it('draws the error line on a refused name, its message in the well under it', async () => {
    const html = await renderInWizard(VariantStep, { ...PROPS, nameRefused: true })
    const name = tagWith(html, 'data-testid="wizard-variant-name"')
    const errorId = name.match(/\saria-describedby="([^"]*-error)\b/)?.[1]

    expect(name).toContain('aria-invalid="true"')
    expect(classesOf(enclosingTag(html, 'data-testid="wizard-variant-name"'))).toContain('kcc-field--error')
    expect(errorId).toBeTruthy()
    expect(tagWith(html, `\\sid="${errorId}"`)).toContain('kcc-well kcc-well--danger')
    expect(html).toContain('«NameTooLong»')
  })

  it('marks no field while no name is refused', async () => {
    const html = await renderInWizard(VariantStep, PROPS)

    expect(html).not.toContain('kcc-field--error')
    expect(html).not.toContain('aria-invalid')
  })
})
