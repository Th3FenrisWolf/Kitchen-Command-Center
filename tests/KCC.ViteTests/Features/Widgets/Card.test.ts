import { describe, expect, it } from 'vitest'
import Card from '~/Widgets/Card/Card.Component.vue'
import { sheetTearFor } from '~/Utilities/BrandColor'
import { renderSsr, expectNoRetiredMarkup } from '../../support/ssr'
import { chosenInk, inkOf, washFields } from '../../support/washes'

const KEY = 'home-card:6f1c2d9e-3b4a-4c5d-8e7f-0a1b2c3d4e5f'

const render = (props: Record<string, unknown> = {}) =>
  renderSsr(Card, { washKey: KEY, ...props }, { default: () => 'Weeknight', drawer: () => 'Hover to open.' })

// Both elements are found by a hook — the drawer by `data-card-drawer`, the slip by its kit class. A
// missing hook is reported here rather than as an empty class list failing some unrelated-looking colour
// assertion.
const classesOf = (html: string, pattern: RegExp, what: string) => {
  const tag = html.match(pattern)?.[0]
  expect(tag, `no element carrying ${what} was rendered`).toBeDefined()
  return (tag!.match(/class="([^"]*)"/)?.[1] ?? '').split(/\s+/)
}

const drawerClasses = (html: string) => classesOf(html, /<div[^>]*data-card-drawer[^>]*>/, 'data-card-drawer')

const slipClasses = (html: string) => classesOf(html, /<div class="kcc-slip[^"]*"[^>]*>/, 'kcc-slip')

// The anchors Vue prints around a slot and a `v-if` are not part of the markup an assertion is about.
const withoutComments = (html: string) => html.replace(/<!--.*?-->/g, '')

describe('Card structure', () => {
  it('rests on one blank, with the standard turn', async () => {
    const html = await render()

    expect((html.match(/kcc-blank/g) ?? []).length).toBe(1)
    expect(html).not.toContain('kcc-slip--wide')
  })

  it('is a slip whose hover group rides on the tilt', async () => {
    const html = await render()

    expect(slipClasses(html)).toEqual(expect.arrayContaining(['kcc-slip', 'group/card']))
    expect(html).toContain('<div class="kcc-torn">')
    expect(html).toContain('<div class="kcc-sheet">')
  })

  // Two rules keep the drawer's percentage height working now that the card is paper. `kcc-slip--fill`
  // carries the parent's height down to the sheet, so the percentage has a definite box — against an
  // auto-height parent it computes to auto and every drawer renders open. The flex column then lets the
  // open drawer shrink into what the heading leaves, rather than running 100% of the sheet past its
  // bottom edge, where the tear cuts the link off.
  it('passes its parent height down so the drawer has a box to collapse against', async () => {
    const html = await render()

    expect(slipClasses(html)).toContain('kcc-slip--fill')
    expect(html).toContain('<div class="flex h-full flex-col">')
  })

  // The type sits on the sheet's pencil rule, so nothing wraps or offsets it. An offset that only hover undoes
  // stays on a phone, where `group-hover` does not apply.
  it.each([
    ['with a drawer', () => render()],
    ['without one', () => renderSsr(Card, { washKey: KEY }, { default: () => 'Weeknight' })],
  ])('sets the content on the rule %s', async (_, renderCard) => {
    const html = withoutComments(await renderCard())

    expect(html).toContain('<div class="flex h-full flex-col">Weeknight')
  })
})

describe('Card wash', () => {
  it('blooms as a card, its first wash in the editor colour', async () => {
    const fields = washFields(await render({ cardColor: 'bg-peach' }))

    expect(fields).toHaveLength(1)
    expect([1, 2]).toContain(fields[0]!.length)
    expect(inkOf(fields[0]![0])).toBe(chosenInk('peach'))
  })

  it('takes its arrangement from the key the page hands it', async () => {
    const once = washFields(await render({ cardColor: 'bg-peach' }))
    const again = washFields(await render({ cardColor: 'bg-peach' }))
    const other = washFields(await render({ cardColor: 'bg-peach', washKey: 'home-card:another' }))

    expect(again).toEqual(once)
    expect(other[0]!.map((wash) => wash.id)).not.toEqual(once[0]!.map((wash) => wash.id))
  })

  it('leaves a paper ground bare, its wash unset', async () => {
    expect(await render({ cardColor: 'bg-paper' })).not.toContain('kcc-wash')
  })

  it('leaves a card the editor gave no colour bare', async () => {
    expect(await render()).not.toContain('kcc-wash')
  })

  // Razor writes `card-color=""` for a content item with no BackgroundColor, which never reaches the
  // prop default.
  it('leaves an empty colour bare', async () => {
    expect(await render({ cardColor: '' })).not.toContain('kcc-wash')
  })

  // Until the migrated content is restored into the database, a stored card can still name a retired
  // hue. The sheet then renders bare paper rather than a `var(--color-rosewater)` that resolves to nothing.
  it('leaves a retired hue bare', async () => {
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
    const html = await renderSsr(Card, { washKey: KEY }, { default: () => 'Weeknight' })

    expect(html).not.toContain('data-card-drawer')
  })
})

describe('Card remnants', () => {
  it('leaves no Softbound class behind', async () => {
    const html = await render({ cardColor: 'bg-peach' })

    expectNoRetiredMarkup(html)
  })
})
