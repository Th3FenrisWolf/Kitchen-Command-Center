import { defineComponent, h, provide, type App, type Component } from 'vue'
import { BLOOM_KEY, useBloom, type Bloom } from '~/Bloom/bloomContext'
import type { GlobalComponent } from '~/GlobalComponents'
import type { IslandEntry, ServerIslands } from './payload'

const refuse = (name: string, what: string) =>
  new Error(`${name} is an island, so Razor may pass it props only, but it received ${what}.`)

function claimingInto(keys: string[], bloom: Bloom): Bloom {
  return {
    ...bloom,
    claimKey(key) {
      const claimed = bloom.claimKey(key)
      keys.push(claimed)
      return claimed
    },
  }
}

function islandWrapper({ name, component }: GlobalComponent, output: ServerIslands): Component {
  return defineComponent({
    name: `${name}Island`,
    inheritAttrs: false,
    setup(_, { attrs, slots }) {
      if (Object.keys(slots).length > 0) throw refuse(name, 'slot content')
      const fn = Object.keys(attrs).find((key) => typeof attrs[key] === 'function')
      if (fn) throw refuse(name, `a function as "${fn}"`)

      const entry: IslandEntry = { name, props: { ...attrs }, keys: [] }
      const index = output.islands.push(entry) - 1
      provide(BLOOM_KEY, claimingInto(entry.keys, useBloom()))

      return () => h('kcc-island', { 'data-island': index }, [h(component, attrs)])
    },
  })
}

export function registerIslands(app: App, registry: readonly GlobalComponent[]): ServerIslands {
  const output: ServerIslands = { islands: [], components: [] }
  const paths = new Map<unknown, string>(registry.map(({ component, path }) => [component, path]))

  app.mixin({
    beforeCreate() {
      const path = paths.get(this.$.type)
      if (path && !output.components.includes(path)) output.components.push(path)
    },
  })

  for (const entry of registry) {
    const component = entry.island ? islandWrapper(entry, output) : entry.component
    app.component(entry.name, component)
    app.component(entry.name.toLowerCase(), component)
  }

  return output
}
