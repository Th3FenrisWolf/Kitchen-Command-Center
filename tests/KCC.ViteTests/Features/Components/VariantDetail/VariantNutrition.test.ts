import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import VariantNutrition from '~/Components/VariantDetail/VariantNutrition.vue'

const full = {
  calories: 520,
  proteinG: 30,
  carbsG: 50,
  fatG: 20,
  saturatedFatG: 6,
  fiberG: 4,
  sugarG: 12,
  sodiumMg: 480,
}

const render = (over: Record<string, unknown> = {}) => renderSsr(VariantNutrition, { ...full, ...over })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

const bars = (html: string) => html.match(/<span[^>]*bg-peach[^>]*>/g) ?? []

describe('VariantNutrition sheet', () => {
  it('is a plain sheet on the second tear, labelled with the wheat', async () => {
    const html = await render()

    expect(tagWith(html, 'kcc-slip')).toContain('kcc-slip kcc-tear-2')
    expect(html).toContain('<span class="kcc-label"><i class="fa-duotone fa-wheat" aria-hidden="true"></i>')
    expect(html).toMatch(/kcc-label[^>]*>.*?Nutrition/s)
    // The figures run the full width of the sheet, so there is no margin for a wash to sit under.
    expect(html).not.toContain('kcc-wash')
    expect(html).not.toMatch(/sk-[a-z]/)
  })

  it('keeps the section heading for assistive tech and kicks off with the serving basis', async () => {
    const html = await render()

    expect(html).toMatch(/<h2 class="sr-only">\s*Nutrition\s*<\/h2>/)
    expect(html).toMatch(/class="kcc-kick">\s*PerServing\s*</)
    expect((html.match(/kcc-label/g) ?? []).length).toBe(1)
  })

  it('sets the four headline figures as a stats row, the unit small beside each', async () => {
    const html = await render()

    expect(html).toMatch(/<div class="kcc-stats[^"]*">/)
    expect(html).toMatch(/<p class="kcc-lbl">Calories<\/p><p class="kcc-v">\s*520\s*(?:<!--v-if-->)?<\/p>/)
    expect(html).toMatch(/<p class="kcc-lbl">Protein<\/p><p class="kcc-v">\s*30\s*<small>g<\/small><\/p>/)
    expect(html).toContain('<p class="kcc-lbl">Carbs</p>')
    expect(html).toContain('<p class="kcc-lbl">Fat</p>')
    expect((html.match(/class="kcc-v"/g) ?? []).length).toBe(4)
    // Saturated fat, fiber, sugar and sodium are the two-column list below, not headline stats.
    expect(html).not.toContain('<p class="kcc-lbl">Sodium</p>')
  })

  it('draws one peach bar per macro, each the width of its share of the grams', async () => {
    const html = await render()

    expect(bars(html)).toHaveLength(3)
    expect(bars(html)[0]).toContain('width:30%')
    expect(bars(html)[1]).toContain('width:50%')
    expect(bars(html)[2]).toContain('width:20%')
    expect(bars(html)[0]).toContain('h-1.5')
    expect(html).not.toMatch(/bg-peach[^"]*rounded/)
    expect(html).toMatch(/<p class="kcc-kick">Protein<\/p>/)
  })

  it('omits the bars when no macro gram was recorded', async () => {
    const html = await render({ proteinG: null, carbsG: null, fatG: null })

    expect(bars(html)).toHaveLength(0)
    expect(html).toContain('520')
  })

  it('draws no bar for a zero-gram macro, so no kick label sits over an empty band', async () => {
    const html = await render({ fatG: 0 })

    expect(bars(html)).toHaveLength(2)
    expect(bars(html)[0]).toContain('width:37.5%')
    expect(bars(html)[1]).toContain('width:62.5%')
    expect(html).not.toMatch(/<p class="kcc-kick">Fat<\/p>/)
    expect(html).toContain('<p class="kcc-lbl">Fat</p>')
  })

  it('skips the stats row when only a secondary figure was recorded', async () => {
    const html = await renderSsr(VariantNutrition, { sodiumMg: 480 })

    expect(html).not.toContain('kcc-stats')
    expect(bars(html)).toHaveLength(0)
    expect(html).toMatch(/<dt class="kcc-lbl">Sodium<\/dt>/)
  })

  it('lists the remaining figures two-column, values in Sono with a small-caps unit', async () => {
    const html = await render()

    expect(html).toMatch(/<dt class="kcc-lbl">Fiber<\/dt><dd class="kcc-num[^"]*">\s*4\s*<span class="kcc-unit">g<\/span>/)
    expect(html).toMatch(
      /<dt class="kcc-lbl">Sodium<\/dt><dd class="kcc-num[^"]*">\s*480\s*<span class="kcc-unit">mg<\/span>/,
    )
    expect(html).toContain('SaturatedFat')
    expect(html).toContain('Sugar')
  })

  it('leaves out a figure the cook never recorded', async () => {
    const html = await render({ fiberG: null })

    expect(html).not.toContain('Fiber')
    expect(html).toContain('Sugar')
  })

  it('says so in body copy when nothing at all was recorded', async () => {
    const html = await renderSsr(VariantNutrition, {})

    expect(html).toMatch(/class="kcc-body[^"]*">\s*NutritionNotProvided\s*</)
    expect(html).not.toContain('kcc-stats')
    expect(bars(html)).toHaveLength(0)
  })

  it('carries no Softbound hook, weight or paper radius', async () => {
    const html = await render()

    expect(html).not.toContain('sk-')
    expect(html).not.toContain('font-bold')
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
  })
})
