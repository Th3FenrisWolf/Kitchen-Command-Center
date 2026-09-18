import { describe, expect, it } from 'vitest'
import WizardProgress from '~/Components/Wizard/WizardProgress.vue'
import { renderSsr } from '../../../support/renderSsr'

const render = (current: number, total: number) => renderSsr(WizardProgress, { current, total })

// Completed steps read as ink, the current step is the one marker pill, and steps still ahead stay
// ink-soft (the kcc-kick default) — counted by exact class rather than looked for anywhere in the markup,
// so a stray class on the wrong step fails the count.
const inkCount = (html: string) => (html.match(/class="kcc-kick text-ink"/g) ?? []).length
const softCount = (html: string) => (html.match(/class="kcc-kick text-ink-soft"/g) ?? []).length
const pillCount = (html: string) => (html.match(/class="kcc-kick rounded-md bg-marker px-3 text-marker-ink"/g) ?? []).length

describe('WizardProgress', () => {
  it('reads completed steps as ink, the current step as a marker pill, and leaves the rest ink-soft', async () => {
    const html = await render(2, 5)

    expect(inkCount(html)).toBe(1)
    expect(pillCount(html)).toBe(1)
    expect(softCount(html)).toBe(3)
  })

  it('marks every earlier step ink on the last step, with none left ink-soft', async () => {
    const html = await render(4, 4)

    expect(inkCount(html)).toBe(3)
    expect(pillCount(html)).toBe(1)
    expect(softCount(html)).toBe(0)
  })

  it('announces the current step to assistive tech on a numbered list', async () => {
    const html = await render(2, 5)

    expect(html).toContain('<ol')
    expect((html.match(/<li/g) ?? []).length).toBe(5)
    expect((html.match(/aria-current="step"/g) ?? []).length).toBe(1)
  })
})
