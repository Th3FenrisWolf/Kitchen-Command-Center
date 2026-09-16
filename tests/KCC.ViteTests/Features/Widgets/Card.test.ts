import { createSSRApp, h } from 'vue'
import { renderToString } from '@vue/server-renderer'
import { describe, expect, it } from 'vitest'
import Card from '~/Widgets/Card/Card.Component.vue'

const render = (props: Record<string, unknown> = {}) =>
  renderToString(
    createSSRApp({
      render: () => h(Card, props, { default: () => 'Weeknight', drawer: () => 'Hover to open.' }),
    }),
  )

// The drawer is found by its hook, never by a visual class, so a restyle cannot break the test. A missing
// hook is reported here rather than as an empty class list failing some unrelated-looking color assertion.
const drawerClasses = (html: string) => {
  const tag = html.match(/<div[^>]*data-card-drawer[^>]*>/)?.[0]
  expect(tag, 'no element carrying data-card-drawer was rendered').toBeDefined()
  return (tag!.match(/class="([^"]*)"/)?.[1] ?? '').split(/\s+/)
}

describe('Card drawer colors', () => {
  it('inverts the card colors by default', async () => {
    const classes = drawerClasses(await render())

    expect(classes).toContain('bg-ink')
    expect(classes).toContain('text-paper')
  })

  it('inverts a caller-supplied pair of card colors', async () => {
    const classes = drawerClasses(await render({ cardColor: 'bg-peach', cardTextColor: 'text-ink-on-wash' }))

    expect(classes).toContain('bg-ink-on-wash')
    expect(classes).toContain('text-peach')
  })

  it('prefers explicit drawer colors over the inversion', async () => {
    const classes = drawerClasses(await render({ drawerColor: 'bg-teal', drawerTextColor: 'text-ink-on-wash' }))

    expect(classes).toContain('bg-teal')
    expect(classes).toContain('text-ink-on-wash')
  })

  it('omits the drawer when the slot is unfilled', async () => {
    const html = await renderToString(createSSRApp({ render: () => h(Card, null, { default: () => 'Weeknight' }) }))

    expect(html).not.toContain('data-card-drawer')
  })
})
