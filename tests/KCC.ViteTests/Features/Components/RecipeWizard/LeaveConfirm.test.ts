import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { classesOf, enclosingTag } from '../../../support/recipeWizard'

// The dialog teleports to body after mount, so the server render holds none of it; the template is read instead.
const template = readFileSync(
  fileURLToPath(new URL('../../../../../src/KCC.Web/Features/Components/RecipeWizard/LeaveConfirm.vue', import.meta.url)),
  'utf8',
).replace(/^[\s\S]*?<template>/, '')

describe('LeaveConfirm', () => {
  it('wraps Stay and Leave onto the next rule, never 12px under', () => {
    expect(classesOf(enclosingTag(template, 'data-testid="leave-stay"'))).toEqual(
      expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-rule-1']),
    )
  })
})
