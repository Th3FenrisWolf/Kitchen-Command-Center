import { describe, expect, it } from 'vitest'
import Stacker from '~/Widgets/Stacker/Stacker.Component.vue'
import { markStuckCards } from '~/Widgets/Stacker/markStuckCards'
import { renderSsr, expectNoRetiredMarkup } from '../../support/ssr'

const card = (heading: string, backgroundColor: string, tear?: 1 | 2 | 3 | 4 | 5 | 6) => ({
  heading,
  subHeading: `${heading}, in one line.`,
  backgroundColor,
  tear,
})

const render = (cards: ReturnType<typeof card>[]) => renderSsr(Stacker, { cards })

const tagsOf = (html: string, hook: string) => [...html.matchAll(new RegExp(`<div ${hook}[^>]*>`, 'g'))].map(([tag]) => tag)

const slipClasses = (html: string) =>
  [...html.matchAll(/<div class="(kcc-slip[^"]*)"/g)].map(([, classes]) => classes!.split(/\s+/))

const fakeSlip = (...startingClasses: string[]) => {
  const classes = new Set(startingClasses)
  const classList = {
    toggle: (name: string, force?: boolean) => {
      const on = force ?? !classes.has(name)
      if (on) classes.add(name)
      else classes.delete(name)
      return on
    },
  }
  return { classes, classList }
}

const sentinelEntry = (top: number, slip: ReturnType<typeof fakeSlip>) => ({
  target: { nextElementSibling: slip },
  boundingClientRect: { top },
})

describe('Stacker structure', () => {
  it('tears one sheet per card, cycling the six presets', async () => {
    const html = await render(
      ['Prep', 'Cook', 'Plate', 'Chill', 'Bake', 'Rest', 'Serve'].map((heading) => card(heading, 'bg-paper')),
    )

    expect(html.match(/kcc-slip/g)).toHaveLength(7)
    expect([...html.matchAll(/kcc-tear-(\d)/g)].map(([, preset]) => Number(preset))).toEqual([1, 2, 3, 4, 5, 6, 1])
  })

  it("takes a card's own tear when the page hands it one", async () => {
    const html = await render([card('Prep', 'bg-paper', 5), card('Cook', 'bg-paper', 6)])

    expect([...html.matchAll(/kcc-tear-(\d)/g)].map(([, preset]) => Number(preset))).toEqual([5, 6])
  })

  it('sets the heading and its line on the sheet', async () => {
    const html = await render([card('Prep', 'bg-paper')])

    expect(html).toContain('<h3 class="kcc-h4">Prep</h3>')
    expect(html).toContain('<p class="kcc-body">Prep, in one line.</p>')
  })
})

describe('Stacker sticky mechanics', () => {
  it('pins each card 32px lower than the one before it, below the pad, and marks the last', async () => {
    const cards = tagsOf(await render([card('Prep', 'bg-paper'), card('Cook', 'bg-paper')]), 'data-card')

    expect(cards).toHaveLength(2)
    expect(cards[0]).toContain('style="top: calc(32px + var(--pad-offset, 0px))"')
    expect(cards[1]).toContain('style="top: calc(64px + var(--pad-offset, 0px))"')
    expect(cards[0]).toContain('class="sticky transition-[top]"')
    expect(cards[1]).toContain('last')
  })

  it('keeps a sentinel a pixel above where each card sticks', async () => {
    const sentinels = tagsOf(await render([card('Prep', 'bg-paper'), card('Cook', 'bg-paper')]), 'data-sentinel')

    expect(sentinels).toHaveLength(2)
    expect(sentinels[0]).toContain('class="absolute size-0 transition-[top]"')
    expect(sentinels[0]).toContain('style="top: calc(-33px - var(--pad-offset, 0px))"')
    expect(sentinels[1]).toContain('style="top: calc(-65px - var(--pad-offset, 0px))"')
  })

  // The observer toggles `.stuck` on the sentinel's next sibling, so the slip has to be that sibling and
  // has to carry the shrink itself. Tailwind's `scale-*` sets the `scale` property, which composes with the
  // slip's `rotate` transform instead of replacing it.
  it('shrinks the slip that follows the sentinel, and only until it is the last', async () => {
    const html = await render([card('Prep', 'bg-paper')])

    expect(html).toMatch(/<div data-sentinel class="absolute size-0 transition-\[top\]"[^>]*><\/div><div class="kcc-slip/)
    expect(slipClasses(html)[0]).toEqual(
      expect.arrayContaining(['origin-top', 'transition-all', 'duration-100', '[.stuck]:scale-95', '[.last_div]:scale-100']),
    )
  })
})

describe('Stacker stuck marking', () => {
  it('marks every card in a batch whose sentinel is above the viewport', () => {
    const first = fakeSlip()
    const second = fakeSlip()

    markStuckCards([sentinelEntry(-1, first), sentinelEntry(-120, second)])

    expect(first.classes.has('stuck')).toBe(true)
    expect(second.classes.has('stuck')).toBe(true)
  })

  it('marks one card and clears another in a single batch', () => {
    const pinned = fakeSlip()
    const released = fakeSlip('stuck')

    markStuckCards([sentinelEntry(-1, pinned), sentinelEntry(0, released)])

    expect(pinned.classes.has('stuck')).toBe(true)
    expect(released.classes.has('stuck')).toBe(false)
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

  // Until the migrated content is restored into the database, a stored card can still name a retired
  // hue. The sheet then renders bare paper rather than a `var(--color-rosewater)` that resolves to nothing.
  it('leaves a retired hue and a paper ground unwashed', async () => {
    expect(await render([card('Prep', 'bg-rosewater')])).not.toContain('kcc-wash')
    expect(await render([card('Prep', 'bg-paper')])).not.toContain('kcc-wash')
  })
})

describe('Stacker remnants', () => {
  it('leaves no Softbound class behind', async () => {
    const html = await render([card('Prep', 'bg-peach'), card('Cook', 'bg-rosewater')])

    expectNoRetiredMarkup(html)
    expect(html).not.toContain('aspect-square')
    expect(html).not.toContain('text-center')
  })

  it('pours the card colour into the wash instead of painting the sheet with it', async () => {
    const html = await render([card('Prep', 'bg-peach'), card('Cook', 'bg-rosewater')])

    expect(html).not.toContain('bg-peach')
    expect(html).not.toContain('bg-rosewater')
  })
})
