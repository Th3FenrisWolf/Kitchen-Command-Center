import type { Component, ComponentOptions } from 'vue'
import type {} from '~/Islands/serverOnly'

export interface GlobalComponent {
  name: string
  path: string
  component: Component
  island: boolean
}

export interface LazyGlobalComponent {
  name: string
  path: string
  load: () => Promise<Component>
}

const lazyModules = import.meta.glob<{ default: Component }>('./**/*.Component.vue')

export function componentName(path: string): string | null {
  const match = path.match(/\/([^/]+)\.Component\.vue$/)
  return match?.[1] ?? null
}

export const registryPath = (globKey: string) => `Features/${globKey.replace(/^(\.\.?\/)+/, '')}`

export const isIsland = (component: Component) => !(component as ComponentOptions).serverOnly

export const lazyGlobals: readonly LazyGlobalComponent[] = Object.entries(lazyModules).flatMap(([key, load]) => {
  const name = componentName(key)
  return name ? [{ name, path: registryPath(key), load: () => load().then((module) => module.default) }] : []
})

// HTML parsers lowercase custom element tags, but Vue templates use PascalCase, so each entry answers to both.
export const byTag = <T extends { name: string }>(entries: readonly T[]) =>
  new Map(entries.flatMap((entry) => [[entry.name, entry] as const, [entry.name.toLowerCase(), entry] as const]))

const lazyByTag = byTag(lazyGlobals)

export const lazyGlobal = (tag: string) => lazyByTag.get(tag)
