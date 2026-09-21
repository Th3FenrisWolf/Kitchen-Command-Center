import { describe, expect, it } from 'vitest'
import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
import { renderSsr } from '../../support/renderSsr'

const SLOTS = {
  eyebrow: () => 'Kitchen notes',
  title: () => 'Account settings',
  'action-button': () => 'Back to profile',
  description: () => 'Everything the kitchen knows about you.',
}

const render = (props: Record<string, unknown> = {}, slots: Record<string, () => unknown> = SLOTS) =>
  renderSsr(SmallHero, props, slots)

// `ssrRenderSlot` wraps every slot's output in Fragment anchors, unconditionally. Dropping them lets an
// assertion name an element and the copy inside it in one readable string.
const printed = (html: string) => html.replaceAll('<!--[-->', '').replaceAll('<!--]-->', '')

const slipClasses = (html: string) => {
  const tag = html.match(/<section[^>]*>/)?.[0]
  expect(tag, 'the hero rendered no <section> slip').toBeDefined()
  return (tag!.match(/class="([^"]*)"/)?.[1] ?? '').split(/\s+/)
}

describe('SmallHero structure', () => {
  it('is a hero-torn section, taped, padded to 48px', async () => {
    const html = await render()

    expect(slipClasses(html)).toEqual(expect.arrayContaining(['kcc-slip', 'kcc-tear-hero']))
    expect(html).toContain('<div class="kcc-torn"><div class="kcc-sheet" style="--pad:48px;">')
    expect(html).toContain('<span class="kcc-tape" aria-hidden="true"></span>')
  })

  // Razor hands the widget's margins down as a class. They belong on the slip, where the tilt is, and they
  // have to arrive exactly once: a component that both inherits its attributes and re-binds `$attrs` on the
  // root prints every class twice.
  it('rides the margins Razor gives it on the slip, once', async () => {
    const classes = slipClasses(await render({ class: 'mt-12 mb-4' }))

    expect(classes).toEqual(expect.arrayContaining(['kcc-slip', 'kcc-tear-hero', 'mt-12', 'mb-4']))
    expect(classes.filter((name) => name === 'mt-12')).toHaveLength(1)
  })
})

describe('SmallHero wash', () => {
  it('pools peach in the bottom-right corner, clear of the eyebrow', async () => {
    const html = await render()

    expect(html).toContain(
      '<span class="kcc-wash" style="--c:var(--color-peach);--x:100%;--y:100%;--w:34%;--h:min(60%, 68px);" aria-hidden="true">',
    )
    expect(html.match(/kcc-wash/g)).toHaveLength(1)
  })
})

describe('SmallHero copy', () => {
  it('kicks the eyebrow, sets the title in display type and the description in body', async () => {
    const html = printed(await render())

    expect(html).toContain('<p class="kcc-kick">Kitchen notes</p>')
    expect(html).toContain('<h1 class="kcc-h3">Account settings</h1>')
    expect(html).toContain('<p class="kcc-body mt-6">Everything the kitchen knows about you.</p>')
  })

  it('sets the action beside the title on one wrapping row', async () => {
    const html = printed(await render())
    const row = html.match(/<div class="flex flex-wrap[^"]*">([\s\S]*?)<\/div>/)

    expect(row?.[0], 'the title and its action are not on a row').toContain('items-center')
    expect(row?.[0]).toContain('justify-between')
    expect(row?.[0]).toContain('gap-x-7')
    expect(row?.[1]).toContain('<h1 class="kcc-h3">Account settings</h1>')
    expect(row?.[1]).toContain('Back to profile')
  })

  it('leaves out the eyebrow and the description when nothing fills them', async () => {
    const html = await render({}, { title: () => 'Create a recipe' })

    expect(html).not.toContain('kcc-kick')
    expect(html).not.toContain('kcc-body')
  })
})

describe('SmallHero ramp', () => {
  // `dark` outlived the two-ground world it switched; persisted widget configuration still carries it, so
  // the prop is accepted and changes nothing.
  it('accepts the dark flag without branching the sheet', async () => {
    expect(await render({ dark: true })).toEqual(await render({ dark: false }))
  })
})

describe('SmallHero remnants', () => {
  it('leaves no Softbound class behind', async () => {
    const html = await render()

    expect(html).not.toMatch(/sk-[a-z]/)
    expect(html).not.toMatch(/rounded-(?:lg|xl|2xl|3xl)/)
    expect(html).not.toContain('data-ink')
  })
})
