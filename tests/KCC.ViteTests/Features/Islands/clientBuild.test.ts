import { readFileSync } from 'node:fs'
import { createRequire } from 'node:module'
import { fileURLToPath } from 'node:url'
import type { Alias, PluginOption, UserConfig } from 'vite'
import { describe, expect, it } from 'vitest'
import viteConfig from '../../../../src/KCC.Web/vite.config'

const web = (path: string) => fileURLToPath(new URL(`../../../../src/KCC.Web/${path}`, import.meta.url))

type Env = { command: 'build' | 'serve'; mode: string; isSsrBuild: boolean; isPreview: boolean }
const config = (env: Pick<Env, 'command' | 'mode'>) =>
  (viteConfig as unknown as (env: Env) => UserConfig)({ ...env, isSsrBuild: env.mode === 'ssr', isPreview: false })

const client = config({ command: 'build', mode: 'production' })
const ssr = config({ command: 'build', mode: 'ssr' })
const fallback = config({ command: 'build', mode: 'fallback' })
const devServer = config({ command: 'serve', mode: 'development' })

const vueAlias = (built: UserConfig) =>
  (built.resolve?.alias as Alias[]).find(({ find }) => find instanceof RegExp && find.test('vue'))?.replacement

const pluginNames = (built: UserConfig) =>
  (built.plugins as PluginOption[])
    .flat(Infinity)
    .flatMap((plugin) => (plugin && typeof plugin === 'object' && 'name' in plugin ? [plugin.name] : []))

const asValue = (value: unknown) => JSON.parse(String(value)) as unknown

describe('the client build', () => {
  it('resolves vue to the package default, which is the runtime-only build', () => {
    expect(vueAlias(client)).toBeUndefined()

    const vuePackage = createRequire(web('package.json')).resolve('vue/package.json')
    const { module } = JSON.parse(readFileSync(vuePackage, 'utf8')) as { module: string }
    expect(module).toMatch(/vue\.runtime\.esm-bundler\.js$/)
  })

  it('turns off the Options API and the production devtools and mismatch details', () => {
    const define = client.define ?? {}
    expect(asValue(define.__VUE_OPTIONS_API__)).toBe(false)
    expect(asValue(define.__VUE_PROD_DEVTOOLS__)).toBe(false)
    expect(asValue(define.__VUE_PROD_HYDRATION_MISMATCH_DETAILS__)).toBe(false)
  })
})

type ChunkGroup = { name: string; test: RegExp }

const headerGroup = () => {
  const output = client.build?.rollupOptions?.output as { codeSplitting?: { groups?: ChunkGroup[] } } | undefined
  return output?.codeSplitting?.groups?.find(({ name }) => name === 'HeaderImports')
}

describe('the header chunk', () => {
  it('holds what the header imports statically, so a visit fetches one script for them', () => {
    const group = headerGroup()
    expect(group).toBeDefined()
    for (const module of [
      'Features/Components/Header/useRecentlyViewed.ts',
      'Features/Components/Account/SignOutForm.vue',
      'Features/Components/Account/signOut.ts',
      'Features/Components/Account/SignOutForm.vue?vue&type=script&setup=true&lang.ts',
      'Features/Components/Sheet/KccSheet.vue',
      'Features/Components/Sheet/KccWashes.Component.vue',
    ]) {
      expect(group!.test.test(`/repo/src/KCC.Web/${module}`), module).toBe(true)
    }
  })

  it('leaves the header itself out, so the manifest still keys it by its component path', () => {
    const group = headerGroup()
    expect(group).toBeDefined()
    for (const module of [
      'Features/Components/Header/AppHeader.Component.vue',
      'Features/Pages/AddVariant/AddVariantView.Component.vue',
      'Features/Components/Sheet/KccSheetThing.vue',
      'Features/Components/Header/AppHeaderOther.ts',
    ]) {
      expect(group!.test.test(`/repo/src/KCC.Web/${module}`), module).toBe(false)
    }
  })
})

describe('the builds that compile templates', () => {
  it('keep the full vue build for the SSR build, the dev server and the fallback', () => {
    for (const built of [ssr, devServer, fallback]) {
      expect(vueAlias(built)).toBe('vue/dist/vue.esm-bundler.js')
    }
  })
})

describe('the fallback build', () => {
  it('builds Features/Fallback.ts with a manifest of its own into wwwroot/fallback', () => {
    const rollupInput = fallback.build?.rollupOptions?.input
    expect(Object.values(rollupInput as Record<string, string>)).toEqual([web('Features/Fallback.ts')])
    expect(fallback.build?.outDir).toBe('wwwroot/fallback')
    expect(fallback.build?.manifest).toBe(true)
    expect(fallback.build?.emptyOutDir).toBe(true)
  })

  it('serves its files from /fallback/, where they are written', () => {
    expect(fallback.base).toBe('/fallback/')
  })

  it('leaves the client build output alone', () => {
    expect(pluginNames(fallback)).not.toContain('clean-assets')
    expect(fallback.publicDir).toBe(false)
  })
})
