// @vitest-environment happy-dom
import { readFileSync } from 'node:fs'
import { basename, join } from 'node:path'
import { describe, expect, it } from 'vitest'
import { createApp, h, type Component } from 'vue'
import { laterStep, laterStepNames, prefetchLaterSteps } from '~/Components/RecipeWizard/wizardSteps'
import { arrivedPart } from '~/Islands/laterParts'
import { files } from '../../../support/files'

const FEATURES = join(import.meta.dirname, '../../../../../src/KCC.Web/Features')
const WIZARD = join(FEATURES, 'Components/RecipeWizard')
const FIRST_STEPS = ['StartStep']

const STEPS_ON_DISK = [...files(WIZARD, /Step\.vue$/)]
  .map((path) => basename(path, '.vue'))
  .filter((name) => !FIRST_STEPS.includes(name))
  .sort()

function mountPending(step: Component) {
  const host = document.createElement('div')
  document.body.append(host)
  const app = createApp({ render: () => h(step, { tear: 4, values: {} }) })
  app.mount(host)
  return { host, unmount: () => (app.unmount(), host.remove()) }
}

describe('the wizard later steps', () => {
  it('glob every *Step.vue in the wizard folder except the step every wizard opens on', () => {
    expect(STEPS_ON_DISK.length).toBeGreaterThan(0)
    expect([...laterStepNames].sort()).toEqual(STEPS_ON_DISK)
  })

  it('throw on a step name the glob does not hold', () => {
    expect(() => laterStep('NoSuchStep')).toThrow(/NoSuchStep/)
    for (const name of FIRST_STEPS) expect(() => laterStep(name)).toThrow(new RegExp(name))
  })

  it('are never imported statically by any file under Features', () => {
    const later = new RegExp(
      `^\\s*import\\s+[\\w{},\\s]+\\s+from\\s+['"][^'"]*\\b(${STEPS_ON_DISK.join('|')})\\.vue['"]`,
      'm',
    )
    const importers = [...files(FEATURES, /\.(vue|ts)$/)].filter((path) => later.test(readFileSync(path, 'utf8')))

    expect(importers).toEqual([])
  })

  it("leave the parts a wizard reaches only after its first step out of the wizard's own import", () => {
    const wizard = readFileSync(join(WIZARD, 'RecipeWizard.vue'), 'utf8')

    for (const part of ['ReadingState.vue', 'SubmittedSheet.vue', 'PhotoStrip.vue', 'LeaveConfirm.vue', 'photoEncoding']) {
      const staticImport = new RegExp(
        `^\\s*import\\s+(?!type\\b)[\\w{},\\s]+\\s+from\\s+['"]\\./${part.replace('.', '\\.')}['"]`,
        'm',
      )
      expect(wizard, part).not.toMatch(staticImport)
    }
  })

  it('show a pending step as the step sheet with its tear, empty and busy, before its chunk arrives', () => {
    const { host, unmount } = mountPending(laterStep(STEPS_ON_DISK[0]!))

    const slip = host.querySelector('.kcc-slip')
    expect(slip?.getAttribute('aria-busy')).toBe('true')
    expect(slip?.classList.contains('kcc-tear-4')).toBe(true)
    expect(slip?.querySelector('.kcc-sheet')).not.toBeNull()
    expect(host.textContent?.trim()).toBe('')
    expect(host.querySelector('.kcc-label, h2, form, button, input')).toBeNull()

    unmount()
  })

  it('resolve every later step when prefetched, so a reader who reaches one sees it in the same tick', async () => {
    await prefetchLaterSteps()

    for (const name of STEPS_ON_DISK) expect(arrivedPart(laterStep(name)), name).toBeTruthy()
  })
})
