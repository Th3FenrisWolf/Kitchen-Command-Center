import { describe, expect, it } from 'vitest'
import WizardProgress from '~/Components/Wizard/WizardProgress.vue'
import { renderSsr } from '../../../support/renderSsr'

const render = (current: number, total: number) => renderSsr(WizardProgress, { current, total })

// The fill is what tells a user where they are, so it is asserted by counting marker segments rather than
// by looking for the class anywhere in the markup — a single stray `bg-marker` would satisfy the latter.
const markerCount = (html: string) => (html.match(/bg-marker/g) ?? []).length
const restCount = (html: string) => (html.match(/bg-desk-2/g) ?? []).length

describe('WizardProgress', () => {
  it('fills one segment per completed step and leaves the rest', async () => {
    const html = await render(2, 5)

    expect(markerCount(html)).toBe(2)
    expect(restCount(html)).toBe(3)
  })

  it('fills every segment on the last step', async () => {
    const html = await render(4, 4)

    expect(markerCount(html)).toBe(4)
    expect(restCount(html)).toBe(0)
  })

  it('announces position to assistive tech', async () => {
    const html = await render(2, 5)

    expect(html).toContain('role="progressbar"')
    expect(html).toContain('aria-valuenow="2"')
    expect(html).toContain('aria-valuemin="1"')
    expect(html).toContain('aria-valuemax="5"')
  })
})
