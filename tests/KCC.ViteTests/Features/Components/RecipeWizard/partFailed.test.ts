// @vitest-environment happy-dom
import { afterEach, describe, expect, it } from 'vitest'
import { createApp, h, ref, type Component } from 'vue'
import PhotoStripPending from '~/Components/RecipeWizard/PhotoStripPending.vue'
import { STEP_SHEET } from '~/Components/RecipeWizard/stepFlip'
import StepPending from '~/Components/RecipeWizard/StepPending.vue'
import SubmittedPending from '~/Components/RecipeWizard/SubmittedPending.vue'
import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
import { wizardStrings } from '../../../support/recipeWizard'

function mount(pending: Component, error?: Error, attrs: Record<string, unknown> = {}) {
  const host = document.createElement('div')
  document.body.append(host)
  createApp({
    setup() {
      provideResourceStrings(wizardStrings(), 'RecipeWizard')
      return () => h(pending, { error, ...attrs })
    },
  })
    .component('ResourceString', ResourceString)
    .mount(host)
  return host
}

describe.each([
  ['a later step', StepPending],
  ['the submitted sheet', SubmittedPending],
  ['the photo strip', PhotoStripPending],
])('%s that never arrives', (_name, pending) => {
  afterEach(() => {
    document.body.innerHTML = ''
  })

  it('is pending and busy while its chunk is on the way', () => {
    const host = mount(pending)

    expect(host.querySelector('[data-later-pending][aria-busy="true"]')).not.toBeNull()
    expect(host.querySelector('[role="alert"]')).toBeNull()
  })

  it('shows the error and a Try again control in place of the pending state once it fails', () => {
    const host = mount(pending, new Error('chunk blocked'))

    expect(host.querySelector('[data-later-pending]')).toBeNull()
    expect(host.querySelector('[aria-busy]')).toBeNull()
    expect(host.querySelector('[role="alert"]')?.textContent).toContain(wizardStrings()['RecipeWizard.UnexpectedError'])
    expect(host.querySelector('[data-test="later-part-retry"]')?.textContent).toContain(
      wizardStrings()['RecipeWizard.TryAgain'],
    )
  })
})

describe('a failed part', () => {
  afterEach(() => {
    document.body.innerHTML = ''
  })

  it.each([
    ['a later step', StepPending],
    ['the submitted sheet', SubmittedPending],
    ['the photo strip', PhotoStripPending],
  ])('prints %s error on paper', (_name, pending) => {
    const host = mount(pending, new Error('chunk blocked'))

    expect(host.querySelector('.kcc-sheet [role="alert"]')).not.toBeNull()
    expect(host.querySelector('.kcc-sheet [data-test="later-part-retry"]')).not.toBeNull()
  })

  it('sets the Retry row a rule below the error well, so the button lift stays off the well', () => {
    const host = mount(StepPending, new Error('chunk blocked'))

    expect(host.querySelector('[data-test="later-part-retry"]')?.closest('.kcc-row')?.classList).toContain('mt-rule-1')
  })

  it('keeps the space the wizard sets under the photo strip once it fails', () => {
    const host = mount(PhotoStripPending, new Error('chunk blocked'), { class: 'mb-rule-1' })

    expect(host.firstElementChild?.classList).toContain('mb-rule-1')
  })

  it('turns a pending step the way its step turns', () => {
    const host = mount(StepPending, undefined, { tear: 3, flip: true })

    expect(host.querySelector('.kcc-slip')?.classList).toContain('kcc-slip--flip')
  })
})

describe('a failed photo strip above a step', () => {
  afterEach(() => {
    document.body.innerHTML = ''
  })

  function mountAboveStep(step: number, error?: Error) {
    const stripFailed = ref(false)
    const host = document.createElement('div')
    document.body.append(host)
    const app = createApp({
      setup() {
        provideResourceStrings(wizardStrings(), 'RecipeWizard')
        return () => h(PhotoStripPending, { error })
      },
    })
      .component('ResourceString', ResourceString)
      .provide(STEP_SHEET, { step: ref(step), stripFailed })
    app.mount(host)
    return { host, app, stripFailed }
  }

  it("takes the tear after the step's, turned over when that tear is odd", () => {
    expect(mountAboveStep(4, new Error('chunk blocked')).host.querySelector('.kcc-slip')?.className).toMatch(
      /kcc-slip--flip.*kcc-tear-5|kcc-tear-5.*kcc-slip--flip/,
    )
    expect(mountAboveStep(3, new Error('chunk blocked')).host.querySelector('.kcc-slip')?.classList).toContain('kcc-tear-4')
    expect(mountAboveStep(3, new Error('chunk blocked')).host.querySelector('.kcc-slip--flip')).toBeNull()
  })

  it('tells the wizard while its sheet is shown, and no longer once it is gone', () => {
    const pending = mountAboveStep(4)
    expect(pending.stripFailed.value).toBe(false)

    const failed = mountAboveStep(4, new Error('chunk blocked'))
    expect(failed.stripFailed.value).toBe(true)

    failed.app.unmount()
    expect(failed.stripFailed.value).toBe(false)
  })
})
