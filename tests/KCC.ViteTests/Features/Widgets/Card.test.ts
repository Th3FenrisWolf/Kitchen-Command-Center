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

const drawerClasses = (html: string) => (html.match(/class="([^"]*\brounded-2xl\b[^"]*)"/)?.[1] ?? '').split(/\s+/)

describe('Card drawer colors', () => {
  it('inverts the card colors by default', async () => {
    const classes = drawerClasses(await render())

    expect(classes).toContain('bg-bone')
    expect(classes).toContain('text-surface-500')
  })

  it('inverts a caller-supplied pair of card colors', async () => {
    const classes = drawerClasses(await render({ cardColor: 'bg-peach', cardTextColor: 'text-onyx' }))

    expect(classes).toContain('bg-onyx')
    expect(classes).toContain('text-peach')
  })

  it('prefers explicit drawer colors over the inversion', async () => {
    const classes = drawerClasses(await render({ drawerColor: 'bg-teal', drawerTextColor: 'text-onyx' }))

    expect(classes).toContain('bg-teal')
    expect(classes).toContain('text-onyx')
  })

  it('omits the drawer when the slot is unfilled', async () => {
    const html = await renderToString(createSSRApp({ render: () => h(Card, null, { default: () => 'Weeknight' }) }))

    expect(html).not.toContain('rounded-2xl')
  })
})
