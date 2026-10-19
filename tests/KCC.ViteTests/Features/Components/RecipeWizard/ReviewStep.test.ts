import type { Component } from 'vue'
import { describe, expect, it } from 'vitest'
import BasicsStep from '~/Components/RecipeWizard/BasicsStep.vue'
import IngredientsStep from '~/Components/RecipeWizard/IngredientsStep.vue'
import InstructionsStep from '~/Components/RecipeWizard/InstructionsStep.vue'
import ReviewStep from '~/Components/RecipeWizard/ReviewStep.vue'
import VariantStep from '~/Components/RecipeWizard/VariantStep.vue'
import type { WizardMode } from '~/Components/RecipeWizard/recipeWizardFlow'
import { emptyValues, type Mark, type WizardValues } from '~/Components/RecipeWizard/wizardValues'
import {
  buttonHolding,
  buttonWith,
  classesOf,
  enclosingTag,
  markerButtons,
  renderInWizard,
  unresolvedKeys,
} from '../../../support/recipeWizard'
import { expectNoRetiredMarkup, tagWith } from '../../../support/ssr'

const ENGLISH = {
  OtherRecipesSeen: 'These photos also show: {0}. Import each one on its own.',
  EditSection: 'Edit {0}',
  MatchTitle: 'Is this a new way to make {0}?',
  AddingTo: 'Adding a variant to {0}',
  PossibleDuplicate: 'This looks almost the same as {0}, a variant of {1}.',
}

const READ: WizardValues = {
  ...emptyValues(),
  recipeName: 'Brown Butter Gnocchi',
  recipeDescription: 'Soft gnocchi in brown butter.',
  variantName: 'Classic',
  prepTime: 15,
  cookTime: 20,
  servings: 4,
  ingredients: [{ name: 'Gnocchi', quantity: 500, unit: 'g', isEyeballed: false }],
  instructions: [{ text: 'Brown the butter.' }],
}

const byReader = (kind: Mark['kind'], reason: string): Mark => ({ kind, reason, code: null, arg: null })

const render = (mode: WizardMode, values: WizardValues, otherRecipes: string[] = [], props: Record<string, unknown> = {}) =>
  renderInWizard(ReviewStep, { mode, values, otherRecipes, ...props }, ENGLISH)

const markKinds = (html: string) =>
  (html.match(/<[a-z]+[^>]*data-testid="value-mark"[^>]*>/g) ?? []).map((tag) => tag.match(/data-kind="(\w+)"/)?.[1])

const notices = (html: string) => html.split('«MissingValue»').length - 1

const submit = (html: string) => tagWith(html, 'data-testid="wizard-submit"')

describe('ReviewStep after a read', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render('recipe', READ)

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'data-testid="review-step"')).toContain('kcc-slip kcc-slip--wide kcc-tear-1')
  })

  it('hooks the review on its sheet', async () => {
    expect(tagWith(await render('recipe', READ), 'data-testid="review-step"')).toContain('kcc-slip')
  })

  it('shows every mark, on its value or its row, with its reason', async () => {
    const html = await render('recipe', {
      ...READ,
      servings: 0,
      marks: {
        'recipe.description': byReader('written', 'Not printed on the card'),
        'variant.prepTime': byReader('estimate', 'Printed as 15 to 20'),
        'variant.servings': { kind: 'unclear', reason: null, code: 'outOfRange', arg: null },
      },
      ingredients: [{ ...READ.ingredients[0]!, mark: byReader('unclear', 'Smudged') }],
      instructions: [
        { text: 'Brown the butter.', mark: { kind: 'translated', reason: null, code: 'translated', arg: 'Italian' } },
      ],
    })

    expect(markKinds(html).sort()).toEqual(['estimate', 'translated', 'unclear', 'unclear', 'written'])
    expect(html).toContain('Not printed on the card')
    expect(html).toContain('Printed as 15 to 20')
    expect(html).toContain('Smudged')
  })

  it('keeps a marked ingredient or step on the rule, its mark one rule under it with no gap', async () => {
    const html = await render('recipe', {
      ...READ,
      ingredients: [{ ...READ.ingredients[0]!, mark: byReader('unclear', 'Smudged') }],
      instructions: [{ text: 'Brown the butter.', mark: byReader('estimate', 'Guessed') }],
    })

    expect(classesOf(enclosingTag(html, 'data-kind="unclear"'))).toEqual(expect.arrayContaining(['gap-y-0']))
    expect(classesOf(enclosingTag(html, 'data-kind="estimate"'))).toEqual(expect.arrayContaining(['gap-y-0']))
  })

  it('keeps every section on the rule: a heading row one rule tall, and a stat with its unit on one rule', async () => {
    const html = await render('recipe', READ, [], { changesMatch: true })
    const rows = [
      'data-testid="review-edit-basics"',
      'data-testid="review-edit-variant"',
      'data-testid="review-edit-ingredients"',
      'data-testid="review-edit-instructions"',
      'aria-describedby=',
    ]

    for (const row of rows) {
      const classes = classesOf(enclosingTag(html, row))
      expect(classes).toContain('items-center')
      expect(classes).not.toContain('items-baseline')
    }
    expect(classesOf(tagWith(html, 'class="kcc-v'))).toEqual(['kcc-v', 'h-rule-1'])
  })

  it('sets the recipe and variant names as titles under their labels', async () => {
    const html = await render('recipe', READ)

    expect(html).toContain('<h3 class="kcc-title mt-rule-1">Brown Butter Gnocchi</h3>')
    expect(html).toContain('<h3 class="kcc-title mt-rule-1">Classic</h3>')
  })

  it('asks for each required value that a read left empty, and holds Submit', async () => {
    const html = await render('recipe', {
      ...READ,
      recipeName: '',
      variantName: '  ',
      ingredients: [{ name: '', unit: '', isEyeballed: false }],
    })

    expect(notices(html)).toBe(3)
    expect(submit(html)).toContain('disabled')
  })

  it('asks for a missing recipe description and missing steps as well', async () => {
    const html = await render('recipe', { ...READ, recipeDescription: '', instructions: [{ text: '' }] })

    expect(notices(html)).toBe(2)
    expect(submit(html)).toContain('disabled')
  })

  it('asks for no recipe value in variant mode', async () => {
    const html = await render('variant', { ...READ, recipeName: '', recipeDescription: '', variantName: '' })

    expect(notices(html)).toBe(1)
    expect(submit(html)).toContain('disabled')
  })

  it('submits a complete read with no notice', async () => {
    const html = await render('recipe', READ)

    expect(notices(html)).toBe(0)
    expect(submit(html)).not.toContain('disabled')
  })

  it('names the other recipes the photos also show', async () => {
    const html = await render('recipe', READ, ['Lemon Bars', 'Shortbread'])

    expect(html).toContain('These photos also show: Lemon Bars, Shortbread. Import each one on its own.')
  })

  it('names no other recipe when the photos show one', async () => {
    expect(await render('recipe', READ)).not.toContain('These photos also show')
  })
})

describe('ReviewStep step links', () => {
  const SECTIONS = [
    ['basics', '«ReviewRecipe»'],
    ['variant', '«ReviewFirstVariant»'],
    ['ingredients', '«Ingredients»'],
    ['instructions', '«Instructions»'],
  ] as const

  it.each(SECTIONS)('offers Edit on the %s section in recipe mode, as a text button named for it', async (section, name) => {
    const button = buttonWith(await render('recipe', READ), `data-testid="review-edit-${section}"`)
    const tag = tagWith(button, `data-testid="review-edit-${section}"`)

    expect(tag).toMatch(/^<button /)
    expect(tag).toContain('type="button"')
    expect(classesOf(tag)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--text']))
    expect(tag).toContain(`aria-label="Edit ${name}"`)
    expect(button).toContain('«Edit»')
  })

  it('offers Edit on the variant, ingredients and instructions sections in variant mode, and no basics', async () => {
    const html = await render('variant', READ)

    expect(html).not.toContain('review-edit-basics')
    expect(tagWith(html, 'data-testid="review-edit-variant"')).toContain('aria-label="Edit «VariantInfo»"')
    expect(tagWith(html, 'data-testid="review-edit-ingredients"')).toContain('aria-label="Edit «Ingredients»"')
    expect(tagWith(html, 'data-testid="review-edit-instructions"')).toContain('aria-label="Edit «Instructions»"')
  })

  it('offers Change photos and Read again for an import', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true })
    const change = tagWith(html, 'data-testid="change-photos"')
    const again = tagWith(html, 'data-testid="read-again"')

    expect(change).toMatch(/^<button /)
    expect(buttonWith(html, 'data-testid="change-photos"')).toContain('«ChangePhotos»')
    expect(again).toMatch(/^<button /)
    expect(again).not.toContain('disabled')
    expect(buttonWith(html, 'data-testid="read-again"')).toContain('«ReadAgain»')
  })

  it('wraps Change photos and Read again onto the next rule, never 12px under', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true })

    expect(classesOf(enclosingTag(html, 'data-testid="change-photos"'))).toEqual(
      expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-rule-1']),
    )
  })

  it("wraps the read-again warning's Cancel and Read again onto the next rule", async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true, confirmingRead: true })

    expect(classesOf(enclosingTag(html, 'data-testid="read-again-confirm"'))).toEqual(
      expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-rule-1']),
    )
  })

  it("keeps the read-again warning's buttons a rule below its well", async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true, confirmingRead: true })

    const column = classesOf(enclosingTag(html, 'data-testid="read-again-warning"'))

    expect(column).toEqual(expect.arrayContaining(['flex-col', 'gap-y-rule-1']))
    expect(column).not.toContain('gap-3')
  })

  it('wraps Back and Submit onto the next rule, never 12px under', async () => {
    const html = await render('recipe', READ)

    expect(classesOf(enclosingTag(html, 'data-testid="wizard-submit"'))).toEqual(
      expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-rule-1']),
    )
  })

  it('holds Read again while the flow cannot start a read', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: false })

    expect(tagWith(html, 'data-testid="read-again"')).toContain('disabled')
  })

  it('offers neither Change photos nor Read again for a typed recipe', async () => {
    const html = await render('recipe', READ)

    expect(html).not.toContain('change-photos')
    expect(html).not.toContain('data-testid="read-again"')
  })

  it('warns that reading again replaces the edits while a confirmation is pending, with Read again and Cancel', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true, confirmingRead: true })
    const warning = tagWith(html, 'data-testid="read-again-warning"')

    expect(warning).toMatch(/^<p /)
    expect(warning).toContain('role="alert"')
    expect(classesOf(warning)).toEqual(expect.arrayContaining(['kcc-well', 'kcc-well--warning', 'kcc-kick']))
    expect(html).toMatch(/data-testid="read-again-warning"[^>]*>\s*«ReadAgainWarning»\s*</)
    expect(buttonWith(html, 'data-testid="read-again-confirm"')).toContain('«ReadAgain»')
    expect(classesOf(tagWith(html, 'data-testid="read-again-confirm"'))).toEqual(
      expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']),
    )
    expect(buttonHolding(html, '«ReadingCancel»')).toContain('type="button"')
  })

  it('keeps Submit as the one marker button on the sheet while a read again is pending', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true, confirmingRead: true })
    const markers = markerButtons(html)

    expect(markers).toHaveLength(1)
    expect(markers[0]).toContain('data-testid="wizard-submit"')
  })

  it('marks the review busy and spins beside Submit while the submission is sending', async () => {
    const html = await render('recipe', READ, [], { submitting: true })
    const content = tagWith(html, 'aria-busy="true"')

    expect(content).toMatch(/^<div /)
    expect(html.indexOf(content)).toBeLessThan(html.indexOf('«ReviewRecipe»'))
    expect(buttonWith(html, 'data-testid="wizard-submit"')).toMatch(
      /<i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true"><\/i>[\s\S]*«Submitting»/,
    )
  })

  it('neither marks the review busy nor spins while nothing is sending', async () => {
    const html = await render('recipe', READ)

    expect(html).not.toContain('aria-busy="true"')
    expect(html).not.toContain('fa-circle-notch')
    expect(buttonWith(html, 'data-testid="wizard-submit"')).toContain('«SubmitForReview»')
  })

  it('holds every way off the review and every read while the submission is sending', async () => {
    const html = await render('recipe', READ, [], {
      imported: true,
      canRead: true,
      confirmingRead: true,
      submitting: true,
    })
    const held = [
      'review-edit-basics',
      'review-edit-variant',
      'review-edit-ingredients',
      'review-edit-instructions',
      'change-photos',
      'read-again',
      'read-again-confirm',
    ].map((testid) => tagWith(html, `data-testid="${testid}"`))

    for (const tag of held) expect(tag).toContain('disabled')
    expect(buttonHolding(html, '«Back»').match(/^<button[^>]*>/)?.[0]).toContain('disabled')
  })

  it('holds the read-again confirmation while the flow cannot start a read', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: false, confirmingRead: true })

    expect(tagWith(html, 'data-testid="read-again-confirm"')).toContain('disabled')
  })

  it('leaves the review open while nothing is sending', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true, confirmingRead: true })
    const open = ['review-edit-basics', 'change-photos', 'read-again', 'read-again-confirm'].map((testid) =>
      tagWith(html, `data-testid="${testid}"`),
    )

    for (const tag of open) expect(tag).not.toContain('disabled')
    expect(buttonHolding(html, '«Back»').match(/^<button[^>]*>/)?.[0]).not.toContain('disabled')
  })

  it('shows no warning while no confirmation is pending', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true })

    expect(html).not.toContain('read-again-warning')
    expect(html).not.toContain('read-again-confirm')
  })

  it('resolves every string under the RecipeWizard group, and uses no retired markup', async () => {
    const html = await render('recipe', READ, [], { imported: true, canRead: true, confirmingRead: true })

    expect(unresolvedKeys(html)).toEqual([])
    expectNoRetiredMarkup(html)
  })
})

describe('the steps opened from the review', () => {
  const STEPS: [string, Component, Record<string, unknown>][] = [
    ['basics', BasicsStep, {}],
    ['variant', VariantStep, { mode: 'recipe' }],
    ['ingredients', IngredientsStep, {}],
    ['instructions', InstructionsStep, {}],
  ]

  const renderStep = (step: Component, props: Record<string, unknown>) =>
    renderInWizard(step, { values: READ, canGoBack: true, canProceed: true, ...props })

  it.each(STEPS)('offers Back to review on the %s step opened from the review', async (_step, step, props) => {
    const html = await renderStep(step, { ...props, returnsToReview: true })
    const button = buttonWith(html, 'data-testid="back-to-review"')

    expect(tagWith(button, 'data-testid="back-to-review"')).toContain('type="button"')
    expect(classesOf(tagWith(button, 'data-testid="back-to-review"'))).toEqual(
      expect.arrayContaining(['kcc-btn', 'kcc-btn--ghost']),
    )
    expect(button).toContain('«BackToReview»')
    expect(unresolvedKeys(html)).toEqual([])
  })

  it.each(STEPS)('offers no Back to review on the %s step reached by Next', async (_step, step, props) => {
    expect(await renderStep(step, props)).not.toContain('back-to-review')
  })
})

describe('ReviewStep after a match', () => {
  const DUPLICATE = { variantKey: '44444444-5555-6666-7777-888888888888', variantName: 'Classic', recipeName: 'Gnocchi' }

  const changeMatch = (html: string) => buttonHolding(html, '«ChangeMatch»')

  const changeMatchTag = (html: string) => changeMatch(html).match(/^<button[^>]*>/)?.[0] ?? ''

  it('names the recipe the variant is added to, with Change as a text button that it describes', async () => {
    const html = await render('variant', READ, [], {
      changesMatch: true,
      addingTo: 'Fluffy Buttermilk Pancakes',
    })
    const line = tagWith(html, '>\\s*Adding a variant to Fluffy Buttermilk Pancakes')
    const id = line.match(/\sid="([^"]+)"/)?.[1]
    const change = changeMatchTag(html)

    expect(line).toMatch(/^<p /)
    expect(classesOf(line)).toEqual(['kcc-heading'])
    expect(id).toBeTruthy()
    expect(change).toContain('type="button"')
    expect(classesOf(change)).toEqual(expect.arrayContaining(['kcc-btn', 'kcc-btn--text']))
    expect(change).toContain(`aria-describedby="${id}"`)
  })

  it('says the member keeps a new recipe, with Change as a text button that it describes', async () => {
    const html = await render('recipe', READ, [], { changesMatch: true })
    const line = tagWith(html, '>\\s*«KeepingNewRecipe»')
    const id = line.match(/\sid="([^"]+)"/)?.[1]

    expect(line).toMatch(/^<p /)
    expect(id).toBeTruthy()
    expect(changeMatchTag(html)).toContain(`aria-describedby="${id}"`)
    expect(html).not.toContain('Is this a new way to make')
    expect(html).not.toContain('Adding a variant to')
  })

  it('offers no Change when the flow has no match step', async () => {
    const recipe = await render('recipe', READ)
    const variant = await render('variant', READ)

    for (const html of [recipe, variant]) {
      expect(html).not.toContain('«ChangeMatch»')
      expect(html).not.toContain('Adding a variant to')
    }
  })

  it('holds Change while the submission is sending', async () => {
    const html = await render('variant', READ, [], {
      changesMatch: true,
      addingTo: 'Fluffy Buttermilk Pancakes',
      submitting: true,
    })

    expect(changeMatchTag(html)).toContain('disabled')
  })

  it.each(['recipe', 'variant'] as const)(
    'names a possible duplicate and its recipe in a warning well in %s mode, and still lets the member submit',
    async (mode) => {
      const html = await render(mode, READ, [], { possibleDuplicate: DUPLICATE })
      const notice = tagWith(html, 'data-testid="possible-duplicate"')

      expect(notice).toMatch(/^<p /)
      expect(classesOf(notice)).toEqual(expect.arrayContaining(['kcc-well', 'kcc-well--warning', 'kcc-kick']))
      expect(html).toMatch(
        /data-testid="possible-duplicate"[^>]*>\s*This looks almost the same as Classic, a variant of Gnocchi\.\s*</,
      )
      expect(submit(html)).not.toContain('disabled')
    },
  )

  it('shows no possible duplicate when the read names none', async () => {
    expect(await render('recipe', READ)).not.toContain('possible-duplicate')
  })

  it('resolves every string under the RecipeWizard group', async () => {
    const html = await render('variant', READ, [], {
      changesMatch: true,
      addingTo: 'Fluffy Buttermilk Pancakes',
      possibleDuplicate: DUPLICATE,
    })

    expect(unresolvedKeys(html)).toEqual([])
    expectNoRetiredMarkup(html)
  })
})
