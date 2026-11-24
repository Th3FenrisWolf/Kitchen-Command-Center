// @vitest-environment happy-dom
import { readFileSync } from 'node:fs'
import { join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { createApp, h, nextTick } from 'vue'
import LeaveConfirm from '~/Components/RecipeWizard/LeaveConfirm.vue'
import { classesOf, enclosingTag } from '../../../support/recipeWizard'

// The dialog teleports to body after mount, so the server render holds none of it; the template is read instead.
const template = readFileSync(
  join(import.meta.dirname, '../../../../../src/KCC.Web/Features/Components/RecipeWizard/LeaveConfirm.vue'),
  'utf8',
).replace(/^[\s\S]*?<template>/, '')

describe('LeaveConfirm', () => {
  it('wraps Stay and Leave onto the next rule, never 12px under', () => {
    expect(classesOf(enclosingTag(template, 'data-testid="leave-stay"'))).toEqual(
      expect.arrayContaining(['flex-wrap', 'gap-x-3', 'gap-y-rule-1']),
    )
  })

  it('opens with its first button focused when it mounts already asked, as a later part that a leave outran does', async () => {
    const host = document.createElement('div')
    document.body.append(host)
    const app = createApp({ render: () => h(LeaveConfirm, { open: true }) })
    app.mount(host)
    for (let i = 0; i < 5; i++) await nextTick()

    const dialog = document.querySelector('[data-testid="leave-dialog"]')
    expect(dialog).not.toBeNull()
    expect(document.activeElement).toBe(dialog?.querySelector('button'))

    app.unmount()
    host.remove()
  })
})
