import { describe, expect, it } from 'vitest'
import Card from '~/Widgets/Card/Card.Component.vue'
import { sheetTearFor } from '~/Utilities/BrandColor'
import { renderSsr } from '../../support/renderSsr'

const render = (props: Record<string, unknown> = {}) =>
  renderSsr(Card, props, { default: () => 'Weeknight', drawer: () => 'Hover to open.' })

// Both elements are found by a hook — the drawer by `data-card-drawer`, the slip by the kit class the
// widget wrapper's `:has()` rule also selects. A missing hook is reported here rather than as an empty
// class list failing some unrelated-looking colour assertion.
const classesOf = (html: string, pattern: RegExp, what: string) => {
  const tag = html.match(pattern)?.[0]
  expect(tag, `no element carrying ${what} was rendered`).toBeDefined()
  return (tag!.match(/class="([^"]*)"/)?.[1] ?? '').split(/\s+/)
}

const drawerClasses = (html: string) => classesOf(html, /<div[^>]*data-card-drawer[^>]*>/, 'data-card-drawer')

const slipClasses = (html: string) => classesOf(html, /<div class="kcc-slip[^"]*"[^>]*>/, 'kcc-slip')

describe('Card structure', () => {
  it('is a slip whose group and margins ride on the tilt', async () => {
    const html = await render({ marginClasses: 'mt-12 mb-4' })

    expect(slipClasses(html)).toEqual(expect.arrayContaining(['kcc-slip', 'group/card', 'mt-12', 'mb-4']))
    expect(html).toContain('<div class="kcc-torn">')
    expect(html).toContain('<div class="kcc-sheet">')
  })

  // Two rules keep the drawer's percentage height working now that the card is paper. `kcc-slip--fill`
  // carries the grid cell's height down to the sheet, so the percentage has a definite box — against an
  // auto-height parent it computes to auto and every drawer renders open. The flex column then lets the
  // open drawer shrink into what the heading leaves, rather than running 100% of the sheet past its
  // bottom edge, where the tear cuts the link off.
  it('fills its grid cell so the drawer has a box to collapse against', async () => {
    const html = await render()

    expect(slipClasses(html)).toContain('kcc-slip--fill')
    expect(html).toContain('<div class="flex h-full flex-col">')
  })

  it('nudges the heading up as the drawer opens', async () => {
    const classes = classesOf(await render(), /<div class="relative top-1[^"]*"[^>]*>/, 'the heading wrapper')

    expect(classes).toEqual(
      expect.arrayContaining(['relative', 'top-1', 'group-hover/card:top-0', 'group-focus-within/card:top-0']),
    )
  })
})

describe('Card wash', () => {
  it('pools the editor colour under the bottom-right corner', async () => {
    const html = await render({ cardColor: 'bg-peach' })

    expect(html).toContain(
      '<span class="kcc-wash" style="--c:var(--color-peach);--x:85%;--y:90%;--w:55%;--h:50%;" aria-hidden="true">',
    )
    expect(html.match(/kcc-wash/g)).toHaveLength(1)
  })

  it('leaves a paper ground unwashed', async () => {
    expect(await render({ cardColor: 'bg-paper' })).not.toContain('kcc-wash')
  })

  // Content still carries Softbound hues until the CMS migration; an unknown name is no wash rather than
  // a `var(--color-rosewater)` that resolves to nothing.
  it('leaves a retired hue unwashed', async () => {
    expect(await render({ cardColor: 'bg-rosewater' })).not.toContain('kcc-wash')
  })
})

describe('Card tear', () => {
  it('takes the preset a grid hands it', async () => {
    expect(slipClasses(await render({ tear: 3 }))).toContain('kcc-tear-3')
  })

  it('falls back to a stable preset for the seed', async () => {
    const html = await render({ seed: 'Weeknight dinners' })

    expect(slipClasses(html)).toContain(`kcc-tear-${sheetTearFor('Weeknight dinners')}`)
  })
})

describe('Card drawer', () => {
  it('is paper-2 in ink whatever the card is washed with', async () => {
    const classes = drawerClasses(await render({ cardColor: 'bg-peach' }))

    expect(classes).toEqual(expect.arrayContaining(['bg-paper-2', 'text-ink', 'rounded-md']))
    expect(classes).not.toContain('bg-ink')
    expect(classes).not.toContain('text-paper')
  })

  it('keeps its open-on-hover behaviour', async () => {
    const classes = drawerClasses(await render())

    expect(classes).toEqual(
      expect.arrayContaining(['h-[0%]', 'overflow-hidden', 'group-hover/card:h-full', 'focus-within:h-full']),
    )
  })

  it('omits the drawer when the slot is unfilled', async () => {
    const html = await renderSsr(Card, {}, { default: () => 'Weeknight' })

    expect(html).not.toContain('data-card-drawer')
  })
})

describe('Card remnants', () => {
  it('leaves no Softbound class behind', async () => {
    const html = await render({ cardColor: 'bg-peach' })

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
    expect(html).not.toContain('ink-on-wash')
    expect(html).not.toContain('data-ink')
  })
})
