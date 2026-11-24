import type { Component } from 'vue'
import { isIsland, lazyGlobal } from '~/GlobalComponents'

// The client holds no eager registry, so a server-only component is refused once its module has loaded.
export function islandLoader(name: string): () => Promise<Component> {
  const entry = lazyGlobal(name)
  if (!entry) throw new Error(`No island is named ${name}.`)
  return () =>
    entry.load().then((component) => {
      if (!isIsland(component)) throw new Error(`${name} is server-only, so it is never hydrated.`)
      return component
    })
}
