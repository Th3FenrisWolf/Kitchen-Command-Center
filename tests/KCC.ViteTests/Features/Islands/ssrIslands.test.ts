import { createSSRApp, defineComponent, h, type Component } from 'vue'
import { renderToString } from 'vue/server-renderer'
import { describe, expect, it } from 'vitest'
import { BLOOM_KEY, createBloom, useBloom } from '~/Bloom/bloomContext'
import type { GlobalComponent } from '~/GlobalComponents'
import { registerIslands } from '~/Islands/ssrIslands'

const Isle = defineComponent({
  props: { label: { type: String, required: true }, count: Number },
  setup(props) {
    useBloom().claimKey(`isle-${props.label}`)
    useBloom().claimKey('shared')
    return () => h('p', { class: 'isle' }, props.label)
  },
})

const Plain = defineComponent({
  setup(_, { slots }) {
    useBloom().claimKey('plain')
    return () => h('section', slots.default?.())
  },
})

const Outer = defineComponent({
  setup: () => () => h('div', { class: 'outer' }, [h(Isle, { label: 'inner' })]),
})

const entry = (name: string, component: Component, island: boolean): GlobalComponent => ({
  name,
  path: `Features/Test/${name}.Component.vue`,
  component,
  island,
})

const REGISTRY = [entry('Isle', Isle, true), entry('Plain', Plain, false), entry('Outer', Outer, true)]

async function render(template: string) {
  const app = createSSRApp({ template })
  app.provide(BLOOM_KEY, createBloom(1, '/p'))
  const page = registerIslands(app, REGISTRY)
  const html = await renderToString(app)
  return { html, page }
}

describe('the server island wrapper', () => {
  it('wraps each island in <kcc-island data-island="N"> and records it in document order', async () => {
    const { html, page } = await render(`<div><Isle label="one" /><isle label="two" :count="2" /></div>`)

    expect(html).toBe(
      '<div><kcc-island data-island="0"><p class="isle">one</p></kcc-island>' +
        '<kcc-island data-island="1"><p class="isle">two</p></kcc-island></div>',
    )
    expect(page.islands).toEqual([
      { name: 'Isle', props: { label: 'one' }, keys: ['/p|isle-one', '/p|shared'] },
      { name: 'Isle', props: { label: 'two', count: 2 }, keys: ['/p|isle-two', '/p|shared~2'] },
    ])
  })

  it('gives a server-only component no wrapper, and an island in its slot content its own', async () => {
    const { html, page } = await render(`<Plain><Isle label="in" /></Plain>`)

    expect(html).toBe('<section><kcc-island data-island="0"><p class="isle">in</p></kcc-island></section>')
    expect(page.islands).toEqual([{ name: 'Isle', props: { label: 'in' }, keys: ['/p|isle-in', '/p|shared'] }])
  })

  it('leaves an island its own template imports inside the outer island', async () => {
    const { html, page } = await render(`<Outer />`)

    expect(html).toBe('<kcc-island data-island="0"><div class="outer"><p class="isle">inner</p></div></kcc-island>')
    expect(page.islands).toEqual([{ name: 'Outer', props: {}, keys: ['/p|isle-inner', '/p|shared'] }])
  })

  it('lists the registry path of every global component rendered, once each, islands and server-only alike', async () => {
    const { page } = await render(`<div><Plain><Isle label="a" /></Plain><Isle label="b" /><Outer /></div>`)

    expect(page.components).toEqual([
      'Features/Test/Plain.Component.vue',
      'Features/Test/Isle.Component.vue',
      'Features/Test/Outer.Component.vue',
    ])
  })

  it('fails the render, naming the island, when Razor gives it slot content', async () => {
    await expect(render(`<Isle label="x">words</Isle>`)).rejects.toThrow(/Isle.*slot/)
  })

  it('fails the render, naming the island, when Razor gives it a function', async () => {
    await expect(render(`<Isle label="x" :count="() => 1" />`)).rejects.toThrow(/Isle.*function/)
    await expect(render(`<Isle label="x" @click="() => {}" />`)).rejects.toThrow(/Isle.*function/)
  })
})
