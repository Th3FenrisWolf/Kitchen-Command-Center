import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'
import { classesOf, enclosingTag } from '../../../support/recipeWizard'
import { tagWith } from '../../../support/ssr'

// The overlay teleports behind an `isMounted` gate, so the server prints only a placeholder: the header is read
// from the template itself.
const template = readFileSync(
  new URL('../../../../../src/KCC.Web/Features/Pages/VariantDetail/CookMode.vue', import.meta.url),
  'utf8',
)
const header = template.match(/<header[\s\S]*?<\/header>/)?.[0] ?? ''

describe('CookMode header', () => {
  it('starts every item on a line and stacks its rows with no gap, so a wrapped header still ends on a rule', () => {
    expect(classesOf(header.match(/^<header[^>]*>/)?.[0] ?? '')).toEqual([
      'flex',
      'flex-wrap',
      'items-start',
      'justify-between',
      'gap-x-6',
    ])
  })

  it('starts the step count on the first rule at every width', () => {
    expect(classesOf(tagWith(header, 'data-test="cook-progress"'))).toEqual(['kcc-kick'])
  })

  it('starts the close button on the line of its two-rule row, so the control shift lands it', () => {
    expect(classesOf(enclosingTag(header, 'data-test="cook-close"'))).toEqual(['flex', 'min-h-rule-2', 'items-start'])
  })

  it('sets the servings stepper in two rules under its label, its count on the first, three rules in all', () => {
    expect(classesOf(enclosingTag(header, 'data-test="cook-servings-dec"'))).toEqual([
      'flex',
      'min-h-rule-2',
      'items-start',
      'gap-3',
    ])
  })
})

describe('CookMode servings', () => {
  it.each(['cook-servings-dec', 'cook-servings-inc'])(
    'centres %s on the line of the count, which is set at the title size',
    (hook) => {
      const button = header.match(new RegExp(`<Button[^>]*data-test="${hook}"[^>]*>`))?.[0] ?? ''
      expect(classesOf(button)).toContain('[--centre-size:var(--text-title)]')
      expect(header).toMatch(/<span class="kcc-num[^"]*\btext-title\b/)
    },
  )
})

describe('CookMode frame', () => {
  it('rounds its column down to whole rules, so the footer at its foot starts on one', () => {
    expect(template).toContain('<div class="flex h-[round(down,100%,var(--bl))] flex-col">')
  })

  it('wraps the step check two rules below the navigation pills', () => {
    expect(classesOf(template.match(/<footer[^>]*>/)?.[0] ?? '')).toContain('gap-y-rule-1')
  })
})
