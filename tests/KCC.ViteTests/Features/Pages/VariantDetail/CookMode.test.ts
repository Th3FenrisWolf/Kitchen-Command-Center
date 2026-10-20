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
  it('stacks its rows with no gap, so a wrapped header still ends on a rule', () => {
    expect(classesOf(header.match(/^<header[^>]*>/)?.[0] ?? '')).toEqual([
      'flex',
      'flex-wrap',
      'items-center',
      'justify-between',
      'gap-x-6',
    ])
  })

  it('starts the step count on the first rule below 640px, where the row is two rules tall', () => {
    expect(classesOf(tagWith(header, 'data-test="cook-progress"'))).toEqual(['kcc-kick', 'self-start', 'sm:self-auto'])
  })

  it('centres the close button in two rules', () => {
    expect(classesOf(enclosingTag(header, 'data-test="cook-close"'))).toEqual(['flex', 'min-h-rule-2', 'items-center'])
  })

  it('sets the servings stepper in two rules under its label, three rules in all', () => {
    expect(classesOf(enclosingTag(header, 'data-test="cook-servings-dec"'))).toEqual([
      'flex',
      'min-h-rule-2',
      'items-center',
      'gap-3',
    ])
  })
})
