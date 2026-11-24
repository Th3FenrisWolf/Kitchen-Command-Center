import { createApp, createSSRApp, type Component } from 'vue'
import { BLOOM_KEY, type Bloom } from '~/Bloom/bloomContext'
import type { IslandEntry } from '~/Islands/payload'
import type { IslandPayload } from '~/Types/ContentRegions'
import { settleBeforeHydrate } from './beforeHydrate'
import { islandLoader } from './registry'

type Loader = (name: string) => () => Promise<Component>

type Arrival = { element: HTMLElement; entry: IslandEntry; component: Component; arrived: boolean }

// The page stays usable as server HTML, so nothing else would show that this island never hydrated.
function reportUnhydrated(entry: IslandEntry, error: unknown) {
  // eslint-disable-next-line no-console
  console.error(`Island ${entry.name} did not hydrate; its server HTML stays.`, error)
}

async function arrive(element: HTMLElement, payload: IslandPayload, load: Loader): Promise<Arrival | undefined> {
  const entry = payload.islands[Number(element.dataset.island)]
  if (!entry) return undefined

  try {
    const component = await load(entry.name)()
    return { element, entry, component, arrived: await settleBeforeHydrate(component, entry.props, entry.name) }
  } catch (error) {
    reportUnhydrated(entry, error)
    return undefined
  }
}

function mount({ element, entry, component, arrived }: Arrival, payload: IslandPayload, bloom: Bloom) {
  try {
    const app = arrived ? createSSRApp(component, entry.props) : createApp(component, entry.props)
    app.provide('isPreview', payload.isPreview ?? false)
    app.provide(BLOOM_KEY, bloom.replay(entry.keys))
    app.mount(element)
  } catch (error) {
    reportUnhydrated(entry, error)
  }
}

// Every island mounts in one task. An island mounted as it arrives starts a frame with its first changes, and the next
// island waits behind that frame: for seconds where a slow rasteriser is still drawing the page's first paint.
export async function hydrateIslands(payload: IslandPayload, bloom: Bloom, load: Loader = islandLoader): Promise<void> {
  const elements = document.querySelectorAll<HTMLElement>('kcc-island[data-island]')
  const arrivals = await Promise.all([...elements].map((element) => arrive(element, payload, load)))
  for (const arrival of arrivals) {
    if (arrival) mount(arrival, payload, bloom)
  }
}
