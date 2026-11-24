import { existsSync, readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { lazyGlobal, lazyGlobals } from '~/GlobalComponents'
import { islandLoader } from '~/Islands/registry'
import { eagerGlobal, eagerGlobals } from '~/Ssr/ServerComponents'
import { files } from '../../support/files'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const FEATURES = join(WEB, 'Features')

const ON_DISK = [...files(FEATURES, /\.Component\.vue$/)].map((path) => relative(WEB, path).replaceAll('\\', '/')).sort()

const ISLANDS = [
  'AccountSettingsView',
  'AccountView',
  'AddVariantView',
  'AppHeader',
  'CreateRecipeView',
  'LoginView',
  'RecipeDetailView',
  'RecipeSearchView',
  'Stacker',
  'VariantDetailView',
]

const SERVER_ONLY = ['AppFooter', 'Breadcrumbs', 'Card', 'KccWashes', 'ResourceString', 'SmallHero', 'UnderlineLink']

const names = (entries: readonly { name: string }[]) => entries.map(({ name }) => name).sort()

describe('the global component registries', () => {
  it('list every *.Component.vue file under Features by its registry path', () => {
    expect(eagerGlobals.map(({ path }) => path).sort()).toEqual(ON_DISK)
    expect(lazyGlobals.map(({ path }) => path).sort()).toEqual(ON_DISK)
  })

  it('name each component by its file name in both registries', () => {
    for (const { name, path } of [...eagerGlobals, ...lazyGlobals])
      expect(path).toMatch(new RegExp(`/${name}\\.Component\\.vue$`))
    expect(names(lazyGlobals)).toEqual(names(eagerGlobals))
  })

  it('load each lazy entry as its own module, the same component the eager registry holds', async () => {
    for (const { name, load } of lazyGlobals) {
      expect(await load()).toBe(eagerGlobal(name)?.component)
    }
  })

  it('make exactly the header, the stacker and the eight page views islands', () => {
    expect(names(eagerGlobals.filter(({ island }) => island))).toEqual(ISLANDS)
    expect(names(lazyGlobals.filter(({ name }) => eagerGlobal(name)?.island))).toEqual(ISLANDS)
  })

  it('make exactly the footer, card, link, washes, trail, string and small hero server-only', () => {
    expect(names(eagerGlobals.filter(({ island }) => !island))).toEqual(SERVER_ONLY)
  })

  it('resolve a lowercase tag to the same entry', () => {
    for (const { name } of eagerGlobals) {
      expect(eagerGlobal(name.toLowerCase())).toBe(eagerGlobal(name))
      expect(lazyGlobal(name.toLowerCase())).toBe(lazyGlobal(name))
    }
    expect(eagerGlobal('NoSuchComponent')).toBeUndefined()
    expect(lazyGlobal('nosuchcomponent')).toBeUndefined()
  })

  // In development a server-only component's CSS would reach the page only through <style data-ssr-styles>, which
  // Main.ts removes after hydration, and the client never imports the component to bring it back.
  it('keep every server-only component free of a <style> block', () => {
    for (const { path } of eagerGlobals.filter(({ island }) => !island)) {
      expect(readFileSync(join(WEB, path), 'utf8'), path).not.toMatch(/<style[\s>]/)
    }
  })

  it('give the client the lazy loader of an island by its name, and refuse an unknown or server-only name', async () => {
    expect(await islandLoader('Stacker')()).toBe(eagerGlobal('Stacker')?.component)
    expect(() => islandLoader('NoSuchComponent')).toThrow(/NoSuchComponent/)
    await expect(islandLoader('AppFooter')()).rejects.toThrow(/AppFooter/)
  })

  it('keep the eager registry and App.vue out of every module the client entry imports', () => {
    const clientModules = [
      'Features/Main.ts',
      'Features/Islands/hydrate.ts',
      'Features/Islands/registry.ts',
      'Features/GlobalComponents.ts',
    ]
    for (const path of clientModules) {
      const source = readFileSync(join(WEB, path), 'utf8')
      expect(source, path).not.toMatch(/eager:\s*true|ServerComponents|App\.vue/)
    }
  })
})

type ManifestChunk = { src?: string; isDynamicEntry?: boolean; imports?: string[] }
const manifestPath = join(WEB, 'wwwroot/.vite/manifest.json')

describe.runIf(existsSync(manifestPath))('the built client chunk graph', () => {
  const manifest = existsSync(manifestPath)
    ? (JSON.parse(readFileSync(manifestPath, 'utf8')) as Record<string, ManifestChunk>)
    : {}

  const staticClosure = (key: string, seen = new Set<string>()): Set<string> => {
    if (seen.has(key) || !manifest[key]) return seen
    seen.add(key)
    for (const imported of manifest[key].imports ?? []) staticClosure(imported, seen)
    return seen
  }

  it('gives every island its own chunk, loaded only on demand', () => {
    for (const name of ISLANDS) {
      const key = Object.keys(manifest).find((path) => path.endsWith(`/${name}.Component.vue`))
      expect(key, name).toBeDefined()
      expect(manifest[key!].isDynamicEntry, name).toBe(true)
    }
  })

  it('imports no component and no App.vue statically from the entry', () => {
    const closure = [...staticClosure('Features/Main.ts')]
    expect(closure.filter((key) => /\.Component\.vue$|\/App\.vue$/.test(key))).toEqual([])
  })
})

describe('the island entry in Layout.cshtml', () => {
  // Island chunks import shared code from the entry by its bare file name. A version query on the entry's own tag
  // gives it a second module URL, so the browser fetches and runs it twice and hydrates every island twice.
  it('links Features/Main.ts by its file name alone, with no version query', () => {
    const layout = readFileSync(join(FEATURES, 'Pages/Shared/Layout.cshtml'), 'utf8')
    const tag = layout.match(/<script[^>]*vite-src="\/Features\/Main\.ts"[^>]*>/)?.[0]
    expect(tag).toBeDefined()
    expect(tag).not.toMatch(/asp-append-version/)
  })
})
