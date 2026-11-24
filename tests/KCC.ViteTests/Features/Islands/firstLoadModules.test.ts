import { existsSync, readFileSync } from 'node:fs'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const WWWROOT = fileURLToPath(new URL('../../../../src/KCC.Web/wwwroot/', import.meta.url))
const manifestPath = join(WWWROOT, '.vite/manifest.json')

type ManifestChunk = { file: string; imports?: string[] }

// The manifest lists chunks, not the modules inside them, so each chunk's source map names what it carries.
describe.runIf(existsSync(manifestPath))('the modules a page loads before it is usable', () => {
  const manifest = existsSync(manifestPath)
    ? (JSON.parse(readFileSync(manifestPath, 'utf8')) as Record<string, ManifestChunk>)
    : {}

  const staticClosure = (keys: string[], seen = new Set<string>()): Set<string> => {
    for (const key of keys) {
      if (seen.has(key) || !manifest[key]) continue
      seen.add(key)
      staticClosure(manifest[key].imports ?? [], seen)
    }
    return seen
  }

  const modulesOf = (...entries: string[]) =>
    [...staticClosure(entries)].flatMap((key) => {
      const mapPath = join(WWWROOT, `${manifest[key]!.file}.map`)
      // The bundler's own runtime chunk is written without a map, and holds no module of ours.
      if (!existsSync(mapPath)) return []
      const map = JSON.parse(readFileSync(mapPath, 'utf8')) as { sources: string[] }
      return map.sources.map((source) => source.replace(/^(\.\.\/)+/, ''))
    })

  const VARIANT_VIEW = 'Features/Pages/VariantDetail/VariantDetailView.Component.vue'

  it('keeps the later-part loader and the wizard arrival wait out of the entry', () => {
    const modules = modulesOf('Features/Main.ts')
    expect(modules).toContain('Features/Islands/laterSchedule.ts')
    expect(modules).not.toContain('Features/Islands/laterParts.ts')
    expect(modules).not.toContain('Features/Islands/whenArrived.ts')
  })

  it('keeps the search and recipe card builders and the wizard arrival wait off the variant page', () => {
    const modules = modulesOf('Features/Main.ts', VARIANT_VIEW)
    expect(modules).toContain('Features/Islands/laterParts.ts')
    expect(modules).not.toContain('Features/Components/Recipe/recipeCardModel.ts')
    expect(modules).not.toContain('Features/Islands/whenArrived.ts')
  })

  it('loads the members-only note and review field on the variant page only where a member sees it', () => {
    expect(modulesOf('Features/Main.ts', VARIANT_VIEW)).not.toContain('Features/Components/Forms/TextAreaField.vue')
  })
})
