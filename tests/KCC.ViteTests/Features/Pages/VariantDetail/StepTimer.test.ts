import { renderSsr } from '../../../support/renderSsr'
import { describe, expect, it } from 'vitest'
import StepTimer from '~/Pages/VariantDetail/StepTimer.vue'

// Counting down is client state; every render below is the timer as the overlay first prints it.
const render = (over: Record<string, unknown> = {}) =>
  renderSsr(StepTimer, { seconds: 600, label: '10-12 minutes', ...over })

const tagWith = (html: string, needle: string) => html.match(new RegExp(`<[a-z0-9]+[^>]*${needle}[^>]*>`))?.[0] ?? ''

describe('StepTimer', () => {
  it('stays a timer for assistive tech, silent until it is read', async () => {
    const timer = tagWith(await render(), 'role="timer"')

    expect(timer).toContain('aria-live="off"')
    expect(timer).toContain('aria-label="10-12 minutes: 10:00"')
  })

  it('sets the countdown in Sono at 64/72, three rules of the ruling', async () => {
    const html = await render()

    expect(html).toContain('<span class="kcc-num text-[64px] leading-[72px]" data-test="timer-display">10:00</span>')
  })

  it('prints the matched phrase as a kick above the number', async () => {
    const html = await render()

    expect(html).toMatch(/<p class="kcc-kick">\s*10-12 minutes\s*<\/p>/)
    expect(html.indexOf('kcc-kick')).toBeLessThan(html.indexOf('timer-display'))
  })

  it('marks the count with a duotone stopwatch in ink', async () => {
    const html = await render()

    expect(html).toContain('<i class="fa-duotone fa-stopwatch text-2xl" aria-hidden="true"></i>')
    expect(html).not.toContain('fa-solid fa-stopwatch')
    expect(html).not.toContain('rating-ink')
  })

  it('starts and pauses from one square 48px kit pill', async () => {
    const toggle = tagWith(await render(), 'data-test="timer-toggle"')

    expect(toggle).toMatch(/^<button /)
    expect(toggle).toContain('kcc-btn kcc-btn--ghost kcc-btn--lg w-12 shrink-0 px-0')
    expect(toggle).toContain('aria-label="StartTimer"')
    expect(await render()).toContain('<i class="fa-duotone fa-play" aria-hidden="true"></i>')
  })

  it('resets from a second square 48px kit pill, named for a screen reader', async () => {
    const reset = tagWith(await render(), 'data-test="timer-reset"')

    expect(reset).toMatch(/^<button /)
    expect(reset).toContain('kcc-btn kcc-btn--ghost kcc-btn--lg w-12 shrink-0 px-0')
    expect(reset).toContain('aria-label="Reset"')
    expect(await render()).toContain('<i class="fa-duotone fa-rotate-left" aria-hidden="true"></i>')
  })

  it('counts a longer duration in whole minutes and padded seconds', async () => {
    const html = await render({ seconds: 5400, label: '90 minutes' })

    expect(html).toContain('data-test="timer-display">90:00</span>')
  })

  it('is printed on the sheet, not in a pill of its own: no ground, no radius, no weight', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toContain('font-bold')
    expect(html).not.toContain('rounded-full')
    expect(html).not.toContain('bg-paper')
    expect(html).not.toContain('transition-colors')
  })
})
