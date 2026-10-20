import { describe, expect, it } from 'vitest'
import IngredientsStep from '~/Components/RecipeWizard/IngredientsStep.vue'
import { emptyValues } from '~/Components/RecipeWizard/wizardValues'
import { ResourceString } from '~/Components/ResourceStrings'
import { classesOf, describedBy, enclosingTag, markKinds, readerMark, renderInWizard } from '../../../support/recipeWizard'
import { renderSsr, tagWith } from '../../../support/ssr'

const render = () =>
  renderSsr(
    IngredientsStep,
    {
      values: {
        ...emptyValues(),
        ingredients: [
          { name: 'flour', quantity: 0.125, unit: 'cups', isEyeballed: false },
          { name: '', unit: '', isEyeballed: false },
        ],
      },
    },
    undefined,
    { ResourceString },
  )

describe('IngredientsStep', () => {
  it('rests on one blank, with the wide turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-slip--wide kcc-tear-1')
  })

  it('sizes the icon-button tracks of each row to a 32px icon button, not a 36px control', async () => {
    const row = classesOf(tagWith(await render(), 'grid-cols-'))
    const tracks = row.filter((name) => /grid-cols-/.test(name))

    expect(tracks.length).toBe(2)
    for (const track of tracks) {
      expect(track).not.toContain('36px')
      expect(track).toContain('var(--control-sm)')
    }
  })

  it('hooks the name input of every ingredient row', async () => {
    const names = (await render()).match(/<input[^>]*data-testid="wizard-ingredient-name"[^>]*>/g) ?? []

    expect(names).toHaveLength(2)
  })

  it('takes a quantity of any decimal in every row', async () => {
    const html = await render()
    const quantities = html.match(/<input[^>]*type="number"[^>]*>/g) ?? []

    expect(quantities).toHaveLength(2)
    quantities.forEach((quantity) => expect(quantity).toContain('step="any"'))
    expect(html).not.toContain('step="0.01"')
  })

  it('sets every quantity in Sono', async () => {
    const quantities = (await render()).match(/<input[^>]*type="number"[^>]*>/g) ?? []

    expect(quantities).toHaveLength(2)
    quantities.forEach((quantity) => expect(classesOf(quantity)).toContain('kcc-num'))
  })
})

describe('IngredientsStep with an eyeballed ingredient', () => {
  const renderEyeballed = () =>
    renderInWizard(IngredientsStep, {
      values: {
        ...emptyValues(),
        ingredients: [
          { name: 'Salt', quantity: 1, unit: 'tsp', isEyeballed: true },
          { name: 'Flour', quantity: 2, unit: 'cups', isEyeballed: false },
        ],
      },
    })

  const rows = (html: string) => html.match(/<li\b[\s\S]*?<\/li>/g) ?? []

  it('hides the quantity and unit of an eyeballed row and says to taste in their place', async () => {
    const [salt = ''] = rows(await renderEyeballed())

    expect(salt).not.toContain('type="number"')
    expect(salt).not.toContain('aria-label="«Unit»"')
    expect(classesOf(tagWith(salt, '>\\s*«ToTaste»'))).toContain('kcc-kick')
    expect(salt).not.toMatch(/<input[^>]*disabled/)
  })

  it('lays an eyeballed row on the same columns as a measured one, so every remove button lines up', async () => {
    const [salt = '', flour = ''] = rows(await renderEyeballed())
    const row = (html: string) => classesOf(html.match(/^<li\b[^>]*>/)?.[0] ?? '')

    expect(row(salt)).toEqual(expect.arrayContaining(['grid', 'items-start']))
    expect(row(salt)).toEqual(row(flour))
    expect(classesOf(tagWith(salt, '>\\s*«ToTaste»'))).toContain('col-span-2')
  })

  it('gives the name the whole first line on a phone, and its own column beside the rest from sm up', async () => {
    const [salt = '', flour = ''] = rows(await renderEyeballed())

    for (const row of [salt, flour])
      expect(classesOf(enclosingTag(row, 'data-testid="wizard-ingredient-name"'))).toEqual(
        expect.arrayContaining(['col-span-full', 'sm:col-span-1']),
      )
  })

  it('starts each line of controls on a rule in two rules and gives a mark one, so every row and its text land on the rule', async () => {
    const html = await renderEyeballed()
    const [salt = ''] = rows(html)
    const row = classesOf(salt.match(/^<li\b[^>]*>/)?.[0] ?? '')

    expect(row).toEqual(
      expect.arrayContaining([
        'grid-rows-[repeat(2,calc(var(--bl)*2))]',
        'sm:grid-rows-[calc(var(--bl)*2)]',
        'auto-rows-[minmax(var(--bl),auto)]',
        'items-start',
        'gap-x-2',
      ]),
    )
    expect(row.filter((name) => /^(sm:)?gap-y-/.test(name))).toEqual([])
    expect(classesOf(enclosingTag(html, '<li\\b')).filter((name) => /^gap/.test(name))).toEqual([])
  })

  it('sets Add ingredient in a kit row, so the step nav below it starts on the rule', async () => {
    const html = await render()

    expect(classesOf(enclosingTag(html, 'fa-plus', 1))).toEqual(['kcc-row'])
  })

  it('separates the two-line rows of a phone by a rule, and the one-line rows from sm up by nothing more', async () => {
    const [salt = ''] = rows(await renderEyeballed())

    expect(classesOf(salt.match(/^<li\b[^>]*>/)?.[0] ?? '')).toEqual(expect.arrayContaining(['pb-rule-1', 'sm:pb-0']))
  })

  it('keeps the quantity and unit of a measured row', async () => {
    const [, flour = ''] = rows(await renderEyeballed())

    expect(flour).toContain('type="number"')
    expect(flour).toContain('aria-label="«Unit»"')
    expect(flour).not.toContain('«ToTaste»')
  })
})

describe('IngredientsStep after a read', () => {
  const renderRead = () =>
    renderInWizard(IngredientsStep, {
      values: {
        ...emptyValues(),
        ingredients: [
          { name: 'Cornmeal', quantity: 1, unit: 'cup', isEyeballed: false },
          {
            name: 'Buttermilk',
            quantity: 1,
            unit: 'cup',
            isEyeballed: false,
            mark: readerMark('unclear', 'Printed as 1 to 1 1/4'),
          },
        ],
      },
    })

  const rows = (html: string) => html.match(/<li\b[\s\S]*?<\/li>/g) ?? []

  it("shows an ingredient's mark on its own row", async () => {
    const [first = '', second = ''] = rows(await renderRead())

    expect(markKinds(first)).toEqual([])
    expect(markKinds(second)).toEqual(['unclear'])
    expect(second).toContain('Printed as 1 to 1 1/4')
  })

  it('puts a mark under its whole row, straight on the next rule', async () => {
    const [, second = ''] = rows(await renderRead())
    const mark = classesOf(tagWith(second, 'data-testid="value-mark"'))

    expect(mark).toContain('col-span-full')
    expect(mark.filter((name) => /^(sm:)?m[tby]?-/.test(name))).toEqual([])
  })

  it('describes the name of a marked ingredient by its mark', async () => {
    const [, second = ''] = rows(await renderRead())

    expect(describedBy(second, 'data-testid="wizard-ingredient-name"')).toContain('data-kind="unclear"')
  })
})
