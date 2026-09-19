import { describe, expect, it } from 'vitest'
import Stacker from '~/Widgets/Stacker/Stacker.Component.vue'
import { renderSsr } from '../../support/renderSsr'

const card = (heading: string, backgroundColor: string) => ({
  heading,
  subHeading: `${heading}, in one line.`,
  backgroundColor,
})

const render = (cards: ReturnType<typeof card>[]) => renderSsr(Stacker, { cards })

const tagsOf = (html: string, hook: string) => [...html.matchAll(new RegExp(`<div ${hook}[^>]*>`, 'g'))].map(([tag]) => tag)

const slipClasses = (html: string) =>
  [...html.matchAll(/<div class="(kcc-slip[^"]*)"/g)].map(([, classes]) => classes!.split(/\s+/))

describe('Stacker structure', () => {
  it('tears one sheet per card, cycling the six presets', async () => {
    const html = await render(
      ['Prep', 'Cook', 'Plate', 'Chill', 'Bake', 'Rest', 'Serve'].map((heading) => card(heading, 'bg-paper')),
    )

    expect(html.match(/kcc-slip/g)).toHaveLength(7)
    expect([...html.matchAll(/kcc-tear-(\d)/g)].map(([, preset]) => Number(preset))).toEqual([1, 2, 3, 4, 5, 6, 1])
  })

  it('sets the heading and its line on the sheet', async () => {
    const html = await render([card('Prep', 'bg-paper')])

    expect(html).toContain('<h2 class="kcc-h4">Prep</h2>')
    expect(html).toContain('<p class="kcc-body">Prep, in one line.</p>')
  })
})

describe('Stacker sticky mechanics', () => {
  it('pins each card 32px lower than the one before it and marks the last', async () => {
    const cards = tagsOf(await render([card('Prep', 'bg-paper'), card('Cook', 'bg-paper')]), 'data-card')

    expect(cards).toHaveLength(2)
    expect(cards[0]).toContain('style="top: 32px"')
    expect(cards[1]).toContain('style="top: 64px"')
    expect(cards[0]).toContain('class="sticky"')
    expect(cards[1]).toContain('last')
  })

  it('keeps a sentinel a pixel above each card', async () => {
    const sentinels = tagsOf(await render([card('Prep', 'bg-paper'), card('Cook', 'bg-paper')]), 'data-sentinel')

    expect(sentinels).toHaveLength(2)
    expect(sentinels[0]).toContain('class="absolute size-0"')
    expect(sentinels[0]).toContain('style="top: -33px"')
    expect(sentinels[1]).toContain('style="top: -65px"')
  })

  // The observer toggles `.stuck` on the sentinel's next sibling, so the slip has to be that sibling and
  // has to carry the shrink itself. Tailwind's `scale-*` sets the `scale` property, which composes with the
  // slip's `rotate` transform instead of replacing it.
  it('shrinks the slip that follows the sentinel, and only until it is the last', async () => {
    const html = await render([card('Prep', 'bg-paper')])

    expect(html).toMatch(/<div data-sentinel class="absolute size-0"[^>]*><\/div><div class="kcc-slip/)
    expect(slipClasses(html)[0]).toEqual(
      expect.arrayContaining(['origin-top', 'transition-all', 'duration-100', '[.stuck]:scale-95', '[.last_div]:scale-100']),
    )
  })
})

describe('Stacker wash', () => {
  it('pools the editor colour under the bottom-right corner', async () => {
    const html = await render([card('Prep', 'bg-peach')])

    expect(html).toContain(
      '<span class="kcc-wash" style="--c:var(--color-peach);--x:85%;--y:90%;--w:55%;--h:50%;" aria-hidden="true">',
    )
    expect(html.match(/kcc-wash/g)).toHaveLength(1)
  })

  // Cards still carry Softbound hues until the CMS migration; an unknown name is no wash rather than a
  // `var(--color-rosewater)` that resolves to nothing.
  it('leaves a retired hue and a paper ground unwashed', async () => {
    expect(await render([card('Prep', 'bg-rosewater')])).not.toContain('kcc-wash')
    expect(await render([card('Prep', 'bg-paper')])).not.toContain('kcc-wash')
  })
})

describe('Stacker remnants', () => {
  it('leaves no Softbound class behind', async () => {
    const html = await render([card('Prep', 'bg-peach'), card('Cook', 'bg-rosewater')])

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
    expect(html).not.toMatch(/shadow-/)
    expect(html).not.toContain('data-ink')
    expect(html).not.toContain('aspect-square')
    expect(html).not.toContain('text-center')
  })

  it('pours the card colour into the wash instead of painting the sheet with it', async () => {
    const html = await render([card('Prep', 'bg-peach'), card('Cook', 'bg-rosewater')])

    expect(html).not.toContain('bg-peach')
    expect(html).not.toContain('bg-rosewater')
  })
})
