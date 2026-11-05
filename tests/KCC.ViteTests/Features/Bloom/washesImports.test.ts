import { existsSync, readFileSync, statSync } from 'node:fs'
import { builtinModules } from 'node:module'
import { dirname, join, relative, resolve } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const FEATURES = join(WEB, 'Features')
const WASHES = join(FEATURES, 'Bloom/washes.ts')
const BLOB_SHAPES = join(FEATURES, 'Bloom/blobShapes.ts')

const SPECIFIER = /(?:^|[\s;}])(?:import|export)\s+(?:type\s+)?(?:[^'";]*?\s+from\s+)?['"]([^'"]+)['"]/g
const BUILTINS = new Set(builtinModules)
const EXTENSIONS = ['', '.ts', '.vue', '.js', '/index.ts']

function resolveLocal(specifier: string, from: string): string | undefined {
  const base = specifier.startsWith('~/')
    ? join(FEATURES, specifier.slice(2))
    : specifier.startsWith('.')
      ? resolve(dirname(from), specifier)
      : undefined
  if (base === undefined) return undefined
  const found = EXTENSIONS.map((extension) => base + extension).find((path) => existsSync(path) && statSync(path).isFile())
  expect(found, `${specifier} from ${relative(WEB, from)}`).toBeDefined()
  return found
}

function importGraph(entry: string): { files: Set<string>; packages: Map<string, string> } {
  const files = new Set<string>()
  const packages = new Map<string, string>()
  const pending = [entry]
  while (pending.length > 0) {
    const file = pending.pop()!
    if (files.has(file)) continue
    files.add(file)
    for (const [, specifier] of readFileSync(file, 'utf8').matchAll(SPECIFIER)) {
      const local = resolveLocal(specifier!, file)
      if (local === undefined) packages.set(specifier!, relative(WEB, file))
      else pending.push(local)
    }
  }
  return { files, packages }
}

describe('washes.ts ships in the client bundle', () => {
  const { files, packages } = importGraph(WASHES)

  it('imports no Node module, directly or through any module it imports', () => {
    const node = [...packages].filter(([specifier]) => specifier.startsWith('node:') || BUILTINS.has(specifier))
    expect(node.map(([specifier, from]) => `${specifier} in ${from}`)).toEqual([])
  })

  it('reads the blob shapes from blobShapes.ts', () => {
    expect([...files].map((file) => relative(WEB, file))).toContain(relative(WEB, BLOB_SHAPES))
  })
})
