import { readdirSync, readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { RETIRED } from '../../support/retired'

const WEB = fileURLToPath(new URL('../../../../src/KCC.Web/', import.meta.url))
const ROOTS = ['Features', 'uSync/v17'].map((dir) => join(WEB, dir))
const SCAN = /\.(vue|cshtml|ts|cs|css|xml|json|config)$/

// Paths exempt from the scan, relative to src/KCC.Web with forward slashes; a trailing slash allows a
// directory. The set is a ratchet: an entry that no longer hits fails the second test, and a new entry needs a
// reason in the commit message. The scan is textual: a comment that names `sk-sheet` or `font-bold` counts as a
// hit, so reword prose too.
const ALLOWLIST = new Set<string>([])

const covers = (entry: string, path: string) => (entry.endsWith('/') ? path.startsWith(entry) : path === entry)

function* files(dir: string): Generator<string> {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name)
    if (entry.isDirectory()) yield* files(path)
    else if (SCAN.test(entry.name)) yield path
  }
}

// `font-weight` inside an @font-face block is a descriptor naming a face (Hazelnut Bold stays declared for
// content that still carries <strong>), not weight applied to text. Blank those blocks out line by line so
// line numbers in the report stay right and the weight rule only sees applied weight.
const withoutFontFaces = (css: string) => css.replace(/@font-face\s*\{[^}]*\}/g, (block) => block.replace(/[^\n]/g, ' '))

const hits = new Map<string, string[]>()
for (const root of ROOTS) {
  for (const file of files(root)) {
    const path = relative(WEB, file).replaceAll('\\', '/')
    const source = readFileSync(file, 'utf8')
    const lines = (file.endsWith('.css') ? withoutFontFaces(source) : source)
      .split('\n')
      .flatMap((line, i) => (RETIRED.test(line) ? [`${i + 1}: ${line.trim().slice(0, 120)}`] : []))
    if (lines.length) hits.set(path, lines)
  }
}

describe('retired design tokens', () => {
  it('appear nowhere in converted source or persisted CMS content', () => {
    const offending = [...hits]
      .filter(([path]) => ![...ALLOWLIST].some((entry) => covers(entry, path)))
      .flatMap(([path, lines]) => lines.map((line) => `${path}:${line}`))
    expect(offending).toEqual([])
  })

  it('the allowlist only names files that still need it', () => {
    const stale = [...ALLOWLIST].filter((entry) => ![...hits.keys()].some((path) => covers(entry, path)))
    expect(stale).toEqual([])
  })
})
