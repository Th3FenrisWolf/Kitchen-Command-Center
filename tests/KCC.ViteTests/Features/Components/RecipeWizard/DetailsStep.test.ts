// @vitest-environment happy-dom
import { createApp, defineComponent, h, nextTick, reactive } from 'vue'
import { describe, expect, it } from 'vitest'
import DetailsStep from '~/Components/RecipeWizard/DetailsStep.vue'
import { createWizardFlow, editValue, type ValueEdit } from '~/Components/RecipeWizard/recipeWizardFlow'
import { NUTRITION_FIELDS, type DetailsChoices } from '~/Components/RecipeWizard/wizardDetails'
import { emptyValues, type WizardValues } from '~/Components/RecipeWizard/wizardValues'
import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
import KccSheet from '~/Components/Sheet/KccSheet.vue'
import {
  buttonHolding,
  classesOf,
  describedBy,
  enclosingTag,
  markKinds,
  readerMark,
  renderInWizard,
  unresolvedKeys,
  wizardStrings,
} from '../../../support/recipeWizard'
import { renderSsr, tagWith } from '../../../support/ssr'
import { washFields } from '../../../support/washes'

const CHOICES: DetailsChoices = {
  categories: [
    { key: 'cat-breakfast', name: 'Breakfast' },
    { key: 'cat-dinner', name: 'Dinner' },
  ],
  tags: [
    { key: 'tag-spicy', name: 'Spicy', kind: 'Style' },
    { key: 'tag-vegetarian', name: 'Vegetarian', kind: 'Diet' },
    { key: 'tag-gluten-free', name: 'Gluten-Free', kind: 'Diet' },
  ],
}

const UNITS: Record<string, string> = { Kcal: '«Kcal»', Grams: '«Grams»', Milligrams: '«Milligrams»' }

const GIVEN: WizardValues = {
  ...emptyValues(),
  category: 'cat-dinner',
  tags: ['tag-spicy', 'tag-gluten-free'],
  difficulty: 'Medium',
  nutrition: { ...emptyValues().nutrition, calories: '450' },
}

const render = (props: Record<string, unknown> = {}, english?: Record<string, string>) =>
  renderInWizard(DetailsStep, { mode: 'recipe', values: emptyValues(), choices: CHOICES, ...props }, english)

const nextButton = (html: string) => buttonHolding(html, '«Next»')

const radios = (html: string) =>
  [...html.matchAll(/<button[^>]*role="radio"[^>]*>[\s\S]*?<\/button>/g)].map(([radio]) => ({
    label: radio.match(/«(\w+)»/)?.[1],
    checked: /aria-checked="true"/.test(radio),
  }))

describe('DetailsStep', () => {
  describe('sheet', () => {
    it('is the step sheet: wide, crisp, margin-ruled, on one blank, with its hook', async () => {
      const html = await render()
      const slip = tagWith(html, 'data-testid="details-step"')

      expect(classesOf(slip)).toEqual(expect.arrayContaining(['kcc-slip', 'kcc-slip--wide', 'kcc-tear-1']))
      expect(slip).toContain('--r:0')
      expect(html).toContain('kcc-sheet kcc-sheet--margin')
      expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    })

    it('takes the tear and flip the wizard hands it', async () => {
      const slip = tagWith(await render({ tear: 4, flip: true }), 'data-testid="details-step"')

      expect(classesOf(slip)).toEqual(expect.arrayContaining(['kcc-tear-4', 'kcc-slip--flip']))
    })

    it('blooms under the wizard-details wash key', async () => {
      const own = washFields(await render())[0]
      const keyed = washFields(
        await renderSsr(KccSheet, { washKey: 'wizard-details', margin: true, crisp: true, wide: true, tear: 1 }),
      )[0]

      expect(own).toEqual(keyed)
    })

    it('carries a screen-reader heading, its label and one form ending in the step nav', async () => {
      const html = await render()

      expect(html).toMatch(/<h2 class="sr-only">«Details»<\/h2>/)
      expect(html).toMatch(/<span class="kcc-label">[\s\S]*?«Details»/)
      expect((html.match(/<form\b/g) ?? []).length).toBe(1)
      expect(nextButton(html)).toContain('type="submit"')
    })

    it('reads every label from the RecipeWizard strings', async () => {
      const recipe = await render({}, {})
      const variant = await render({ mode: 'variant' }, {})

      expect(unresolvedKeys(recipe)).toEqual([])
      expect(unresolvedKeys(variant)).toEqual([])
      for (const key of ['DetailsHint', 'Category', 'NoCategory', 'DietTags', 'StyleTags', 'Difficulty']) {
        expect(recipe).toContain(`«${key}»`)
      }
      for (const key of ['NutritionPerServing', 'ProposedCategory', 'ProposedTags', 'ProposalHint']) {
        expect(recipe).toContain(`«${key}»`)
      }
    })
  })

  describe('category', () => {
    it('is a select field in mode recipe, None first, then each category, the chosen one selected', async () => {
      const html = await render({ values: GIVEN })
      const select = tagWith(html, 'data-testid="details-category"')
      const options = html.match(/<select[^>]*details-category[^>]*>([\s\S]*?)<\/select>/)?.[1] ?? ''

      expect(select).toMatch(/^<select /)
      expect(classesOf(enclosingTag(html, 'data-testid="details-category"'))).toContain('kcc-field')
      expect([...options.matchAll(/<option value(?:="([^"]*)")?/g)].map(([, value]) => value ?? '')).toEqual([
        '',
        'cat-breakfast',
        'cat-dinner',
      ])
      expect(options).toMatch(/<option value(="")?>«NoCategory»<\/option>/)
      expect(options).toMatch(/<option value="cat-dinner" selected/)
    })

    it('selects None while no category is chosen', async () => {
      const options = (await render()).match(/<select[^>]*details-category[^>]*>([\s\S]*?)<\/select>/)?.[1] ?? ''

      expect(options).toMatch(/<option value(="")? selected>/)
    })

    it('is absent in mode variant, with its proposal', async () => {
      const html = await render({ mode: 'variant' })

      expect(html).not.toContain('details-category')
      expect(html).not.toContain('details-proposed-category')
      expect(html).not.toContain('«Category»')
      expect(html).toContain('data-testid="details-proposed-tags"')
    })
  })

  describe('tags', () => {
    it('lists diet then style, each as a kcc-check fieldset under its legend', async () => {
      const html = await render()
      const fieldsets = [...html.matchAll(/<fieldset[^>]*>([\s\S]*?)<\/fieldset>/g)].map(([, inner]) => inner!)
      const tagSets = fieldsets.filter((inner) => inner.includes('details-tag-'))

      expect(tagSets).toHaveLength(2)
      expect(tagSets[0]).toMatch(/<legend[^>]*>«DietTags»<\/legend>/)
      expect(tagSets[0]).toContain('<ul class="kcc-check">')
      expect([...tagSets[0]!.matchAll(/data-testid="details-tag-([\w-]+)"/g)].map(([, key]) => key)).toEqual([
        'tag-vegetarian',
        'tag-gluten-free',
      ])
      expect(tagSets[1]).toMatch(/<legend[^>]*>«StyleTags»<\/legend>/)
      expect([...tagSets[1]!.matchAll(/data-testid="details-tag-([\w-]+)"/g)].map(([, key]) => key)).toEqual(['tag-spicy'])
    })

    it('draws each tag as an sr-only checkbox with a box and its name, checked when chosen', async () => {
      const html = await render({ values: GIVEN })
      const spicy = tagWith(html, 'data-testid="details-tag-tag-spicy"')
      const vegetarian = tagWith(html, 'data-testid="details-tag-tag-vegetarian"')
      const id = spicy.match(/\sid="([^"]+)"/)?.[1]

      expect(spicy).toMatch(/^<input /)
      expect(spicy).toContain('type="checkbox"')
      expect(classesOf(spicy)).toContain('sr-only')
      expect(spicy).toMatch(/\schecked/)
      expect(vegetarian).not.toMatch(/\schecked/)
      expect(tagWith(html, `for="${id}"[^>]*kcc-box`)).toContain('kcc-box--on')
      expect(html).toMatch(new RegExp(`<label for="${id}">Spicy</label>`))
    })
  })

  describe('difficulty', () => {
    it('is a segmented radio group of None, Easy, Medium and Hard, None checked while unset', async () => {
      const html = await render()
      const group = tagWith(html, 'data-testid="details-difficulty"')

      expect(group).toContain('role="radiogroup"')
      expect(classesOf(group)).toContain('kcc-seg')
      expect(radios(html)).toEqual([
        { label: 'DifficultyNone', checked: true },
        { label: 'DifficultyEasy', checked: false },
        { label: 'DifficultyMedium', checked: false },
        { label: 'DifficultyHard', checked: false },
      ])
    })

    it('checks the chosen level', async () => {
      expect(radios(await render({ values: GIVEN })).filter((radio) => radio.checked)).toEqual([
        { label: 'DifficultyMedium', checked: true },
      ])
    })

    it('is named by its Difficulty label', async () => {
      const html = await render()
      const labelledBy = tagWith(html, 'data-testid="details-difficulty"').match(/\saria-labelledby="([^"]+)"/)?.[1]

      expect(html).toMatch(new RegExp(`<[a-z]+[^>]*\\sid="${labelledBy}"[^>]*>«Difficulty»<`))
    })
  })

  describe('nutrition', () => {
    it('is eight numeric text fields, in order, each with its unit', async () => {
      const html = await render()

      for (const { field, label, unit } of NUTRITION_FIELDS) {
        const input = tagWith(html, `data-testid="details-nutrition-${field}"`)
        expect(input).toMatch(/^<input /)
        expect(input).toContain('inputmode="numeric"')
        expect(input).toContain('type="text"')
        expect(classesOf(input)).toContain('kcc-num')
        expect(classesOf(enclosingTag(html, `data-testid="details-nutrition-${field}"`))).toContain('kcc-field')
        expect(input).toContain(`aria-label="«ValueInUnit»"`)
        expect(html).toContain(`«${label}»`)
        expect(html).toContain(UNITS[unit])
      }
      expect([...html.matchAll(/data-testid="details-nutrition-(\w+)"/g)].map(([, field]) => field)).toEqual(
        NUTRITION_FIELDS.map(({ field }) => field),
      )
    })

    it('names each field with its unit', async () => {
      const html = await render({}, { ValueInUnit: '{0} in {1}', Calories: 'Calories', Kcal: 'kcal' })

      expect(tagWith(html, 'data-testid="details-nutrition-calories"')).toContain('aria-label="Calories in kcal"')
    })

    it('shows the typed text as it is', async () => {
      const html = await render({ values: { ...emptyValues(), nutrition: { ...emptyValues().nutrition, calories: '1.5' } } })

      expect(tagWith(html, 'data-testid="details-nutrition-calories"')).toContain('value="1.5"')
    })

    it('ties an inline error, with the field limit, to an invalid value only', async () => {
      const html = await render(
        { values: { ...emptyValues(), nutrition: { ...emptyValues().nutrition, calories: '1.5', fatG: '12' } } },
        { NutritionInvalid: 'Enter a whole number from 0 to {0}.' },
      )
      const calories = tagWith(html, 'data-testid="details-nutrition-calories"')
      const error = describedBy(html, 'data-testid="details-nutrition-calories"')

      expect(calories).toContain('aria-invalid="true"')
      expect(classesOf(enclosingTag(html, 'data-testid="details-nutrition-calories"'))).toContain('kcc-field--error')
      expect(classesOf(error)).toEqual(expect.arrayContaining(['kcc-well', 'kcc-well--danger']))
      expect(html).toContain('Enter a whole number from 0 to 10000.')
      expect(tagWith(html, 'data-testid="details-nutrition-fatG"')).not.toContain('aria-invalid')
      expect(tagWith(html, 'data-testid="details-nutrition-fatG"')).not.toContain('aria-describedby')
      expect((html.match(/kcc-well--danger/g) ?? []).length).toBe(1)
    })
  })

  describe('Next', () => {
    it('is enabled when the wizard allows it and every value is valid', async () => {
      expect(nextButton(await render({ values: GIVEN, canProceed: true }))).not.toMatch(/\sdisabled/)
    })

    it('is disabled while any nutrition value is invalid', async () => {
      const values = { ...GIVEN, nutrition: { ...GIVEN.nutrition, sodiumMg: '50001' } }

      expect(nextButton(await render({ values, canProceed: true }))).toMatch(/\sdisabled/)
    })
  })

  describe('Back to review', () => {
    it('is enabled when every nutrition value is valid', async () => {
      const html = await render({ values: GIVEN, canProceed: true, returnsToReview: true })

      expect(tagWith(html, 'data-testid="back-to-review"')).not.toMatch(/\sdisabled/)
    })

    it('is disabled while any nutrition value is invalid', async () => {
      const values = { ...GIVEN, nutrition: { ...GIVEN.nutrition, sodiumMg: '50001' } }
      const html = await render({ values, canProceed: true, returnsToReview: true })

      expect(tagWith(html, 'data-testid="back-to-review"')).toMatch(/\sdisabled/)
    })
  })

  describe('proposals', () => {
    it('are text fields of at most 100 characters described by the proposal hint', async () => {
      const html = await render()

      for (const hook of ['details-proposed-category', 'details-proposed-tags']) {
        const input = tagWith(html, `data-testid="${hook}"`)
        expect(input).toMatch(/^<input /)
        expect(input).toContain('maxlength="100"')
        expect(describedBy(html, `data-testid="${hook}"`)).toBeTruthy()
        expect(html.slice(html.indexOf(describedBy(html, `data-testid="${hook}"`)))).toMatch(/^<[^>]*>«ProposalHint»/)
      }
    })

    it('show the typed proposals', async () => {
      const html = await render({ values: { ...emptyValues(), proposedCategory: 'Soups', proposedTags: 'Low sugar' } })

      expect(tagWith(html, 'data-testid="details-proposed-category"')).toContain('value="Soups"')
      expect(tagWith(html, 'data-testid="details-proposed-tags"')).toContain('value="Low sugar"')
    })
  })

  describe('marks', () => {
    const MARKED: WizardValues = {
      ...GIVEN,
      marks: {
        'recipe.category': readerMark('estimate', 'Guessed from the title'),
        'variant.difficulty': readerMark('unclear', 'Smudged'),
        'variant.tag.tag-spicy': readerMark('written', 'Named for you'),
        'variant.nutrition.calories': readerMark('estimate', 'Printed as 450 to 500'),
      },
    }

    it('shows each mark the flow holds and describes its control by it', async () => {
      const html = await render({ values: MARKED })

      expect(markKinds(html).sort()).toEqual(['estimate', 'estimate', 'unclear', 'written'])
      expect(describedBy(html, 'data-testid="details-category"')).toContain('data-kind="estimate"')
      expect(describedBy(html, 'data-testid="details-difficulty"')).toContain('data-kind="unclear"')
      expect(describedBy(html, 'data-testid="details-tag-tag-spicy"')).toContain('data-kind="written"')
      expect(describedBy(html, 'data-testid="details-nutrition-calories"')).toContain('data-kind="estimate"')
      expect(html).toContain('Printed as 450 to 500')
    })

    it('names the reader kinds and what the photos printed, after the control', async () => {
      const html = await render(
        {
          values: {
            ...GIVEN,
            marks: {
              'variant.difficulty': { kind: 'suggested', reason: null, code: 'printedAs', arg: 'intermediate' },
              'variant.nutrition.calories': { kind: 'calculated', reason: null, code: 'printedAs', arg: '1200 kJ' },
            },
          },
        },
        { MarkReasonPrintedAs: 'Printed as {0}' },
      )

      expect(describedBy(html, 'data-testid="details-difficulty"')).toContain('data-kind="suggested"')
      expect(describedBy(html, 'data-testid="details-nutrition-calories"')).toContain('data-kind="calculated"')
      expect(html.replace(/<[^>]*>/g, '')).toContain('Printed as intermediate')
      expect(html.replace(/<[^>]*>/g, '')).toContain('Printed as 1200 kJ')
      expect(html).toContain('<span class="kcc-num">1200 kJ</span>')
    })

    it('sets a tag mark in its row, on a line of its own under the name, with no gap above it', async () => {
      const html = await render({ values: MARKED })
      const markId = tagWith(html, 'data-testid="details-tag-tag-spicy"').match(/\saria-describedby="([^"]+)"/)?.[1]
      const row = enclosingTag(html, `\\sid="${markId}"`)

      expect(row).toMatch(/^<li\b/)
      expect(classesOf(row)).toContain('gap-y-0')
      expect(classesOf(tagWith(html, `\\sid="${markId}"`))).toEqual(expect.arrayContaining(['col-start-2', 'col-span-2']))
    })

    it('shows none on values the member chose', async () => {
      const html = await render({ values: GIVEN })

      expect(markKinds(html)).toEqual([])
      expect(tagWith(html, 'data-testid="details-category"')).not.toContain('aria-describedby')
      expect(tagWith(html, 'data-testid="details-difficulty"')).not.toContain('aria-describedby')
    })
  })

  describe('details changed', () => {
    it('explains a refused list in a well', async () => {
      const html = await render({ detailsChanged: true })
      const well = tagWith(html, 'data-testid="details-changed"')

      expect(classesOf(well)).toEqual(expect.arrayContaining(['kcc-well', 'kcc-kick']))
      expect(html).toContain('«DetailsChanged»')
    })

    it('says nothing otherwise', async () => {
      expect(await render()).not.toContain('details-changed')
    })
  })

  describe('events', () => {
    function mount(props: Record<string, unknown> = {}) {
      const emitted: unknown[][] = []
      const record =
        (name: string) =>
        (...args: unknown[]) =>
          emitted.push([name, ...args])
      const host = document.createElement('div')
      const app = createApp(
        defineComponent({
          setup() {
            provideResourceStrings(wizardStrings(), 'RecipeWizard')
            return () =>
              h(DetailsStep, {
                mode: 'recipe',
                values: emptyValues(),
                choices: CHOICES,
                canProceed: true,
                onEdit: record('edit'),
                onTag: record('tag'),
                onNutrition: record('nutrition'),
                onProposal: record('proposal'),
                onNext: record('next'),
                ...props,
              })
          },
        }),
      )
      app.component('ResourceString', ResourceString)
      app.mount(host)
      return { host, emitted, unmount: () => app.unmount() }
    }

    const typeInto = (input: HTMLInputElement | HTMLSelectElement, value: string, event = 'input') => {
      input.value = value
      input.dispatchEvent(new Event(event))
    }

    it('edits the category, None as null', async () => {
      const { host, emitted, unmount } = mount({ values: GIVEN })
      const select = host.querySelector<HTMLSelectElement>('[data-testid="details-category"]')!

      typeInto(select, 'cat-breakfast', 'change')
      typeInto(select, '', 'change')

      expect(emitted).toEqual([
        ['edit', 'category', 'cat-breakfast'],
        ['edit', 'category', null],
      ])
      unmount()
    })

    it('edits the difficulty, None as null', async () => {
      const { host, emitted, unmount } = mount({ values: GIVEN })
      const radio = (label: string) =>
        [...host.querySelectorAll<HTMLButtonElement>('[role="radio"]')].find((button) => button.textContent === label)!

      radio('«DifficultyHard»').click()
      await nextTick()
      radio('«DifficultyNone»').click()

      expect(emitted).toEqual([
        ['edit', 'difficulty', 'Hard'],
        ['edit', 'difficulty', null],
      ])
      unmount()
    })

    it('toggles a tag by its key', async () => {
      const { host, emitted, unmount } = mount()
      const box = host.querySelector<HTMLInputElement>('[data-testid="details-tag-tag-vegetarian"]')!

      box.checked = true
      box.dispatchEvent(new Event('change'))

      expect(emitted).toEqual([['tag', 'tag-vegetarian', true]])
      unmount()
    })

    it('hands on nutrition and proposals as typed', async () => {
      const { host, emitted, unmount } = mount()

      typeInto(host.querySelector<HTMLInputElement>('[data-testid="details-nutrition-proteinG"]')!, '1.5')
      typeInto(host.querySelector<HTMLInputElement>('[data-testid="details-proposed-category"]')!, 'Soups')
      typeInto(host.querySelector<HTMLInputElement>('[data-testid="details-proposed-tags"]')!, 'Low sugar')

      expect(emitted).toEqual([
        ['nutrition', 'proteinG', '1.5'],
        ['proposal', 'proposedCategory', 'Soups'],
        ['proposal', 'proposedTags', 'Low sugar'],
      ])
      unmount()
    })

    it('emits next once on submit', async () => {
      const { host, emitted, unmount } = mount()

      host.querySelector('form')!.dispatchEvent(new Event('submit', { cancelable: true }))

      expect(emitted).toEqual([['next']])
      unmount()
    })
  })

  describe('picking the value a suggestion holds', () => {
    function mountSuggested() {
      const wizard = reactive(createWizardFlow({ flow: 'recipe', importEnabled: true, choices: CHOICES }))
      Object.assign(wizard.values, {
        category: 'cat-dinner',
        difficulty: 'Medium',
        marks: {
          'recipe.category': readerMark('suggested', 'A main meal'),
          'variant.difficulty': readerMark('suggested', 'A few steps'),
        },
      })
      const host = document.createElement('div')
      const app = createApp(
        defineComponent({
          setup() {
            provideResourceStrings(wizardStrings(), 'RecipeWizard')
            return () =>
              h(DetailsStep, {
                mode: 'recipe',
                values: wizard.values,
                choices: wizard.choices,
                onEdit: (...[field, value]: ValueEdit<'category' | 'difficulty'>) =>
                  editValue(wizard, field, value as never),
              })
          },
        }),
      )
      app.component('ResourceString', ResourceString)
      app.mount(host)
      return { host, wizard, unmount: () => app.unmount() }
    }

    it('shows the suggested category on a hidden copy, so choosing its listed option is a change', async () => {
      const html = await render({
        values: { ...GIVEN, marks: { 'recipe.category': readerMark('suggested', 'A main meal') } },
      })
      const options = [
        ...(html.match(/<select[^>]*details-category[^>]*>([\s\S]*?)<\/select>/)?.[1] ?? '').matchAll(
          /<option([^>]*)>([^<]*)<\/option>/g,
        ),
      ].map(([, attributes, text]) => ({
        text: text!.trim(),
        value: attributes!.match(/value="([^"]*)"/)?.[1],
        selected: /\sselected/.test(attributes!),
        hidden: /\shidden/.test(attributes!),
      }))

      expect(options.filter(({ selected }) => selected)).toEqual([
        { text: 'Dinner', value: 'cat-dinner', selected: true, hidden: true },
      ])
      expect(options.filter(({ hidden }) => !hidden).map(({ text }) => text)).toEqual([
        '«NoCategory»',
        'Breakfast',
        'Dinner',
      ])
    })

    it('clears the category mark when the member picks the suggested category', async () => {
      const { host, wizard, unmount } = mountSuggested()
      const select = host.querySelector<HTMLSelectElement>('[data-testid="details-category"]')!

      ;[...select.options].find((option) => !option.hidden && option.value === 'cat-dinner')!.selected = true
      select.dispatchEvent(new Event('change'))
      await nextTick()

      expect(wizard.values.category).toBe('cat-dinner')
      expect(wizard.values.marks).not.toHaveProperty('recipe.category')
      expect(markKinds(host.innerHTML)).toEqual(['suggested'])
      expect([...select.options].filter((option) => option.value === 'cat-dinner')).toHaveLength(1)
      expect(select.value).toBe('cat-dinner')
      unmount()
    })

    it('clears the difficulty mark when the member picks the suggested level', async () => {
      const { host, wizard, unmount } = mountSuggested()
      const medium = [...host.querySelectorAll<HTMLButtonElement>('[role="radio"]')].find(
        (button) => button.textContent === '«DifficultyMedium»',
      )!

      medium.click()
      await nextTick()

      expect(wizard.values.difficulty).toBe('Medium')
      expect(wizard.values.marks).not.toHaveProperty('variant.difficulty')
      expect(markKinds(host.innerHTML)).toEqual(['suggested'])
      unmount()
    })
  })
})
