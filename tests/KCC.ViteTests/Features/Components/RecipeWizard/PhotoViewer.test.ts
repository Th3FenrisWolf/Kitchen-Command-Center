import { readFileSync } from 'node:fs'
import { describe, expect, it } from 'vitest'
import { classesOf } from '../../../support/recipeWizard'

const template = readFileSync(
  new URL('../../../../../src/KCC.Web/Features/Components/RecipeWizard/PhotoViewer.vue', import.meta.url),
  'utf8',
)

describe('PhotoViewer', () => {
  it('sets the close button on a row two rules tall, from its line start, so the photo below starts on a rule', () => {
    const row = template.match(/<div class="([^"]*)">\s*<Button[^>]*ClosePhoto/)?.[1] ?? ''

    expect(classesOf(`<div class="${row}">`)).toEqual(expect.arrayContaining(['min-h-rule-2', 'items-start']))
  })
})
