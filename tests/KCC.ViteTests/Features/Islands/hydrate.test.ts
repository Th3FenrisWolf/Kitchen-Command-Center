// @vitest-environment happy-dom
import { afterEach, describe, expect, it, vi } from 'vitest'
import { defineComponent, h, inject, onMounted, type Component } from 'vue'
import { BLOOM_KEY, createBloom, useBloom } from '~/Bloom/bloomContext'
import { hydrateIslands } from '~/Islands/hydrate'
import { laterPart, prefetchPart } from '~/Islands/laterParts'
import type { IslandEntry } from '~/Islands/payload'
import type { IslandPayload } from '~/Types/ContentRegions'

const PAGE = '/recipes/'

const mounted: string[] = []
const seen: { name: string; isPreview: unknown; claimed: string; seed: number }[] = []

const island = (name: string) =>
  defineComponent({
    props: { label: { type: String, required: true } },
    setup(props) {
      const bloom = useBloom()
      seen.push({ name, isPreview: inject('isPreview'), claimed: bloom.claimKey('sheet'), seed: bloom.seed })
      onMounted(() => mounted.push(name))
      return () => h('p', { class: name }, props.label)
    },
  })

function page(entries: IslandEntry[], isPreview = false): IslandPayload {
  document.body.innerHTML = entries
    .map(({ name, props }, i) => `<kcc-island data-island="${i}"><p class="${name}">${String(props.label)}</p></kcc-island>`)
    .join('<footer>server only</footer>')
  return { isPreview, bloom: { seed: 9, page: PAGE, pageWashes: 0 }, islands: entries }
}

const entry = (name: string, keys: string[] = []): IslandEntry => ({ name, props: { label: `${name} label` }, keys })

const loaderOf = (components: Record<string, Component | Error>) => (name: string) => () => {
  const found = components[name]
  return found instanceof Error ? Promise.reject(found) : Promise.resolve(found)
}

describe('hydrateIslands', () => {
  afterEach(() => {
    mounted.length = 0
    seen.length = 0
    document.body.innerHTML = ''
    vi.restoreAllMocks()
  })

  it('hydrates every island in place from its own component and props, keeping the server nodes', async () => {
    const payload = page([entry('Header'), entry('Search')])
    const serverNodes = [...document.querySelectorAll('p')]

    await hydrateIslands(payload, createBloom(9, PAGE), loaderOf({ Header: island('Header'), Search: island('Search') }))

    expect(mounted.sort()).toEqual(['Header', 'Search'])
    expect([...document.querySelectorAll('p')]).toEqual(serverNodes)
    expect(document.querySelector('footer')?.textContent).toBe('server only')
  })

  it('starts loading every island before any one has arrived', async () => {
    const payload = page([entry('Header'), entry('Search')])
    const requested: string[] = []
    const pending: Record<string, (component: Component) => void> = {}
    const load = (name: string) => () => {
      requested.push(name)
      return new Promise<Component>((resolve) => (pending[name] = resolve))
    }

    const done = hydrateIslands(payload, createBloom(9, PAGE), load)
    await Promise.resolve()

    expect(requested).toEqual(['Header', 'Search'])
    pending.Search(island('Search'))
    pending.Header(island('Header'))
    await done
    expect(mounted.sort()).toEqual(['Header', 'Search'])
  })

  it('provides the one bloom, replaying each island its server keys, and isPreview to every island', async () => {
    const payload = page([entry('Header', [`${PAGE}|sheet~2`]), entry('Search', [`${PAGE}|sheet`])], true)

    await hydrateIslands(payload, createBloom(9, PAGE), loaderOf({ Header: island('Header'), Search: island('Search') }))

    expect(seen.sort((a, b) => a.name.localeCompare(b.name))).toEqual([
      { name: 'Header', isPreview: true, claimed: `${PAGE}|sheet~2`, seed: 9 },
      { name: 'Search', isPreview: true, claimed: `${PAGE}|sheet`, seed: 9 },
    ])
  })

  it("leaves an island's server HTML when its chunk fails to load, and still hydrates the rest", async () => {
    const error = vi.spyOn(console, 'error').mockImplementation(() => {})
    const payload = page([entry('Header'), entry('Search')])

    await hydrateIslands(
      payload,
      createBloom(9, PAGE),
      loaderOf({ Header: new Error('chunk failed'), Search: island('Search') }),
    )

    expect(mounted).toEqual(['Search'])
    expect(document.querySelector('.Header')?.textContent).toBe('Header label')
    expect(error).toHaveBeenCalledWith(expect.stringContaining('Header'), expect.any(Error))
  })

  it('provides the bloom under its injection key', async () => {
    const payload = page([entry('Probe')])
    let provided: unknown
    const probe = defineComponent({
      props: { label: String },
      setup: (props) => {
        provided = inject(BLOOM_KEY)
        return () => h('p', { class: 'Probe' }, props.label)
      },
    })

    await hydrateIslands(payload, createBloom(9, PAGE), loaderOf({ Probe: probe }))

    expect(provided).toMatchObject({ seed: 9, page: PAGE })
  })

  it('hands beforeHydrate the props under the names the island declares, as Razor writes them in kebab case', async () => {
    const written = { ...entry('Wizard'), props: { label: 'Wizard label', 'photo-import': { enabled: true } } }
    const payload = page([written])
    const beforeHydrate = vi.fn(() => Promise.resolve())

    await hydrateIslands(payload, createBloom(9, PAGE), loaderOf({ Wizard: { ...island('Wizard'), beforeHydrate } }))

    expect(beforeHydrate).toHaveBeenCalledWith({ label: 'Wizard label', photoImport: { enabled: true } })
  })

  it('renders an island afresh when its part before hydration never arrives', async () => {
    const payload = page([entry('Wizard')])
    const serverNode = document.querySelector('p')
    vi.spyOn(console, 'warn').mockImplementation(() => undefined)
    const wizard = { ...island('Wizard'), beforeHydrate: () => Promise.reject(new Error('chunk blocked')) }

    await hydrateIslands(payload, createBloom(9, PAGE), loaderOf({ Wizard: wizard }))

    expect(mounted).toEqual(['Wizard'])
    expect(document.querySelector('p')).not.toBe(serverNode)
    expect(document.querySelector('p')?.textContent).toBe('Wizard label')
  })

  it('shows the failed state of a later part the server rendered and the browser never received', async () => {
    vi.useFakeTimers()
    vi.spyOn(console, 'warn').mockImplementation(() => undefined)
    const Sheet = defineComponent({
      props: { error: Error },
      setup: (props) => () => h('p', { class: props.error ? 'failed' : 'pending' }),
    })
    const part = laterPart(() => Promise.reject(new Error('chunk blocked')), Sheet)
    const wizard = defineComponent({
      beforeHydrate: () => prefetchPart(part),
      setup: () => () => h('div', [h(part)]),
    })
    document.body.innerHTML = '<kcc-island data-island="0"><div><p class="arrived">the step</p></div></kcc-island>'
    const payload: IslandPayload = {
      bloom: { seed: 9, page: PAGE, pageWashes: 0 },
      islands: [{ name: 'Wizard', props: {}, keys: [] }],
    }

    const hydrated = hydrateIslands(payload, createBloom(9, PAGE), loaderOf({ Wizard: wizard }))
    await vi.runAllTimersAsync()
    await hydrated
    await vi.runAllTimersAsync()

    expect(document.querySelector('.arrived')).toBeNull()
    expect(document.querySelector('.failed')).not.toBeNull()
    vi.useRealTimers()
  })
})
