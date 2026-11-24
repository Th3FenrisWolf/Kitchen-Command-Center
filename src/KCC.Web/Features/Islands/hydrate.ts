import { createApp, createSSRApp, type Component } from 'vue'
import { BLOOM_KEY, type Bloom } from '~/Bloom/bloomContext'
import type { IslandPayload } from '~/Types/ContentRegions'
import { settleBeforeHydrate } from './beforeHydrate'
import { islandLoader } from './registry'

type Loader = (name: string) => () => Promise<Component>

async function hydrateIsland(element: HTMLElement, payload: IslandPayload, bloom: Bloom, load: Loader) {
  const entry = payload.islands[Number(element.dataset.island)]
  if (!entry) return

  try {
    const component = await load(entry.name)()
    const arrived = await settleBeforeHydrate(component, entry.props, entry.name)
    const app = arrived ? createSSRApp(component, entry.props) : createApp(component, entry.props)
    app.provide('isPreview', payload.isPreview ?? false)
    app.provide(BLOOM_KEY, bloom.replay(entry.keys))
    app.mount(element)
  } catch (error) {
    // The page stays usable as server HTML, so nothing else would show that this island never hydrated.
    // eslint-disable-next-line no-console
    console.error(`Island ${entry.name} did not hydrate; its server HTML stays.`, error)
  }
}

export async function hydrateIslands(payload: IslandPayload, bloom: Bloom, load: Loader = islandLoader): Promise<void> {
  const elements = document.querySelectorAll<HTMLElement>('kcc-island[data-island]')
  await Promise.all([...elements].map((element) => hydrateIsland(element, payload, bloom, load)))
}
