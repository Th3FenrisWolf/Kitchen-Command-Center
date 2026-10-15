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

    expect(row(salt)).toEqual(expect.arrayContaining(['grid', 'items-center']))
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

  it('steps every row by whole rules: 12px between its lines, the rest of the rule under it', async () => {
    const html = await renderEyeballed()
    const [salt = ''] = rows(html)

    expect(classesOf(salt.match(/^<li\b[^>]*>/)?.[0] ?? '')).toEqual(
      expect.arrayContaining(['gap-x-2', 'gap-y-3', 'pb-9', 'sm:pb-3']),
    )
    expect(classesOf(enclosingTag(html, '<li\\b')).filter((name) => /^gap/.test(name))).toEqual([])
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

  it('puts a mark under its whole row, with the rest of a rule under it', async () => {
    const [, second = ''] = rows(await renderRead())

    expect(classesOf(tagWith(second, 'data-testid="value-mark"'))).toEqual(expect.arrayContaining(['col-span-full', 'mb-3']))
  })

  it('describes the name of a marked ingredient by its mark', async () => {
    const [, second = ''] = rows(await renderRead())

    expect(describedBy(second, 'data-testid="wizard-ingredient-name"')).toContain('data-kind="unclear"')
  })
})
