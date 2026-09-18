import { describe, expect, it } from 'vitest'
import WizardProgress from '~/Components/Wizard/WizardProgress.vue'
import { renderSsr } from '../../../support/renderSsr'

const render = (current: number, total: number) => renderSsr(WizardProgress, { current, total })

// Completed steps read as ink, the current step is the one marker pill, and steps still ahead stay
// ink-soft (the kcc-kick default) — counted by one distinguishing class per state rather than the whole
// class literal, so a stray class on the wrong step still fails the count. `text-ink` is also a prefix of
// `text-ink-soft`, so the completed-step count excludes any match immediately followed by a hyphen.
const inkCount = (html: string) => (html.match(/\btext-ink\b(?!-)/g) ?? []).length
const softCount = (html: string) => (html.match(/text-ink-soft/g) ?? []).length
const pillCount = (html: string) => (html.match(/bg-marker/g) ?? []).length

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
    expect(html).toContain('role="list"')
    expect((html.match(/<li/g) ?? []).length).toBe(5)
    expect((html.match(/aria-current="step"/g) ?? []).length).toBe(1)
  })

  it('puts the marker pill inside the current step only, not a neighbour', async () => {
    const html = await render(2, 5)

    const currentLi = html.match(/<li[^>]*aria-current="step"[^>]*>[\s\S]*?<\/li>/)?.[0] ?? ''
    expect(currentLi).toContain('kcc-kick')
    expect(currentLi).toContain('bg-marker')
    expect(currentLi).toContain('rounded-md')
    expect(currentLi).toContain('px-3')
    expect(currentLi).toContain('text-marker-ink')
  })

  it('leaves the last step without a connector slot, so it sits flush at the end of the rule', async () => {
    const html = await render(2, 5)

    const liTags = html.match(/<li[^>]*>/g) ?? []
    expect(liTags.at(-1) ?? '').not.toContain('flex-1')
  })

  it('hides the zero-padded numeral from assistive tech behind a plain sr-only number', async () => {
    const html = await render(2, 5)

    const pairs = html.match(/<span aria-hidden="true">\d{2,}<\/span><span class="sr-only">\d+<\/span>/g) ?? []
    expect(pairs).toHaveLength(5)
    expect(html).toContain('<span aria-hidden="true">01</span><span class="sr-only">1</span>')
  })
})
