// @vitest-environment happy-dom
import { createApp, defineComponent, h } from 'vue'
import { describe, expect, it } from 'vitest'
import DetailsSummary from '~/Components/RecipeWizard/DetailsSummary.vue'
import type { DetailsChoices } from '~/Components/RecipeWizard/wizardDetails'
import { emptyValues, type WizardValues } from '~/Components/RecipeWizard/wizardValues'
import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
import { classesOf, markKinds, renderInWizard, unresolvedKeys, wizardStrings } from '../../../support/recipeWizard'
import { tagWith } from '../../../support/ssr'

const CHOICES: DetailsChoices = {
  categories: [
    { key: 'cat-breakfast', name: 'Breakfast' },
    { key: 'cat-dinner', name: 'Dinner' },
  ],
  tags: [
    { key: 'tag-vegan', name: 'Vegan', kind: 'Diet' },
    { key: 'tag-quick', name: 'Weeknight', kind: 'Style' },
  ],
}

const GIVEN: WizardValues = {
  ...emptyValues(),
  category: 'cat-dinner',
  tags: ['tag-vegan', 'tag-quick'],
  difficulty: 'Medium',
  nutrition: { ...emptyValues().nutrition, calories: '420', proteinG: '18', sodiumMg: '900' },
  proposedCategory: 'Soups',
  proposedTags: 'Gluten free, Low sugar',
}

const render = (values: WizardValues, props: Record<string, unknown> = {}, english?: Record<string, string>) =>
  renderInWizard(DetailsSummary, { mode: 'recipe', values, choices: CHOICES, ...props }, english)

describe('DetailsSummary', () => {
  it('lists the category, tags, difficulty, given nutrition and both proposals under the kicker', async () => {
    const html = await render(GIVEN)
    const section = html.slice(html.indexOf('data-testid="review-details"'))

    expect(tagWith(html, 'data-testid="review-details"')).not.toBe('')
    expect(section).toContain('«ReviewDetails»')
    expect(section).toContain('Dinner')
    expect(section).toContain('Vegan')
    expect(section).toContain('Weeknight')
    expect(section).toContain('«DifficultyMedium»')
    expect(section).toContain('Soups')
    expect(section).toContain('Gluten free, Low sugar')
    expect(unresolvedKeys(html)).toEqual([])
  })

  it('shows each given nutrition value with its unit and leaves the others out', async () => {
    const html = await render(GIVEN)

    expect(html).toMatch(/«Calories»[\s\S]*?420[\s\S]*?«Kcal»/)
    expect(html).toMatch(/«Protein»[\s\S]*?18[\s\S]*?«Grams»/)
    expect(html).toMatch(/«Sodium»[\s\S]*?900[\s\S]*?«Milligrams»/)
    expect(html).not.toContain('«Fiber»')
    expect(html).not.toContain('«Sugar»')
  })

  it('leaves the category and its proposal out in variant mode', async () => {
    const html = await render(GIVEN, { mode: 'variant' })

    expect(html).not.toContain('Dinner')
    expect(html).not.toContain('Soups')
    expect(html).toContain('Vegan')
    expect(html).toContain('Gluten free')
  })

  it('says no details were given when nothing was', async () => {
    const html = await render(emptyValues())

    expect(html).toContain('«NoDetailsGiven»')
    expect(html).not.toContain('«Calories»')
  })

  it('shows no missing-value warning, with or without details', async () => {
    expect(await render(emptyValues())).not.toContain('«MissingValue»')
    expect(await render(GIVEN)).not.toContain('«MissingValue»')
  })

  it('gives each tag the full width, with its mark on a line of its own below', async () => {
    const html = await render({
      ...GIVEN,
      marks: { 'variant.tag.tag-vegan': { kind: 'estimate', reason: null, code: null, arg: null } },
    })
    const mark = html.indexOf('data-kind="estimate"')
    const item = html.slice(html.lastIndexOf('<li', mark))

    expect(classesOf(item)).toEqual(expect.arrayContaining(['grid-cols-[1fr_auto]']))
    expect(classesOf(tagWith(html, 'data-kind="estimate"'))).toContain('col-span-2')
  })

  it('shows every mark beside its value', async () => {
    const html = await render({
      ...GIVEN,
      marks: {
        'recipe.category': { kind: 'estimate', reason: 'From the title', code: null, arg: null },
        'variant.difficulty': { kind: 'written', reason: null, code: null, arg: null },
        'variant.tag.tag-vegan': { kind: 'estimate', reason: null, code: null, arg: null },
        'variant.nutrition.calories': { kind: 'unclear', reason: null, code: null, arg: null },
      },
    })

    expect(markKinds(html).sort()).toEqual(['estimate', 'estimate', 'unclear', 'written'])
    expect(html).toContain('From the title')
  })

  it('shows the reader kinds with what the photos printed', async () => {
    const html = await render(
      {
        ...GIVEN,
        marks: {
          'recipe.category': { kind: 'suggested', reason: null, code: 'printedAs', arg: 'Supper' },
          'variant.nutrition.calories': { kind: 'calculated', reason: null, code: 'printedAs', arg: '1200 kJ' },
        },
      },
      {},
      { MarkReasonPrintedAs: 'Printed as {0}' },
    )

    expect(markKinds(html).sort()).toEqual(['calculated', 'suggested'])
    expect(html).toContain('Printed as Supper')
    expect(html).toContain('Printed as <span class="kcc-num">1200 kJ</span>')
  })

  it('shows no category after an accepted match, whatever mark the read left on it', async () => {
    const html = await render(
      {
        ...GIVEN,
        category: null,
        proposedCategory: '',
        marks: { 'recipe.category': { kind: 'suggested', reason: 'A main dish', code: null, arg: null } },
      },
      { mode: 'variant' },
    )

    expect(html).not.toContain('«Category»')
    expect(html).not.toContain('A main dish')
  })

  it('shows no category row for a key the lists do not hold, mark or not', async () => {
    const html = await render({
      ...GIVEN,
      category: 'cat-gone',
      marks: { 'recipe.category': { kind: 'suggested', reason: 'A main dish', code: null, arg: null } },
    })

    expect(html).not.toContain('«Category»')
    expect(html).not.toContain('A main dish')
  })

  it('offers Edit that names the section and emits edit details', async () => {
    const html = await render(GIVEN, {})
    expect(tagWith(html, 'data-testid="review-edit-details"')).toContain('type="button"')

    const edits: string[] = []
    const host = document.createElement('div')
    createApp(
      defineComponent({
        setup() {
          provideResourceStrings(wizardStrings({ EditSection: 'Edit {0}' }), 'RecipeWizard')
          return () =>
            h(DetailsSummary, { mode: 'recipe', values: GIVEN, choices: CHOICES, onEdit: (s: string) => edits.push(s) })
        },
      }),
    )
      .component('ResourceString', ResourceString)
      .mount(host)

    const button = host.querySelector<HTMLButtonElement>('[data-testid="review-edit-details"]')!
    expect(button.getAttribute('aria-label')).toBe('Edit «ReviewDetails»')
    button.click()

    expect(edits).toEqual(['details'])
  })
})
