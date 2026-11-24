import type { Component } from 'vue'
import { byTag, componentName, isIsland, registryPath, type GlobalComponent } from '~/GlobalComponents'

// Only the server entry and the fallback entry may import this: in the island build the eager glob would pull every
// component into the entry chunk.
const eagerModules = import.meta.glob<{ default: Component }>('../**/*.Component.vue', { eager: true })

export const eagerGlobals: readonly GlobalComponent[] = Object.entries(eagerModules).flatMap(([key, module]) => {
  const name = componentName(key)
  return name && module.default
    ? [{ name, path: registryPath(key), component: module.default, island: isIsland(module.default) }]
    : []
})

const eagerByTag = byTag(eagerGlobals)

export const eagerGlobal = (tag: string) => eagerByTag.get(tag)
