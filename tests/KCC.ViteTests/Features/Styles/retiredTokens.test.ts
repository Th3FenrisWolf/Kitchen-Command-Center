import { readdirSync, readFileSync } from 'node:fs'
import { join, relative } from 'node:path'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

// The redesign retired these tokens outright — no alias layer. A hit means an unstyled element in production,
// silently: Tailwind emits nothing for an unknown class, and Kentico stores class strings as content.
const RETIRED =
  /(?<![\w-])(?:[a-z-]+(?:\/[a-z0-9-]+)?:)*(?:bg|text|border|fa-primary|fa-secondary|ring|outline|fill|stroke|from|to|via|divide|placeholder|decoration|accent|caret)-(?:bone(?:-dark)?|onyx(?:-light)?|surface-\d{3}|overlay-\d{3})(?![\w-])|(?<![\w-])(?:[a-z-]+:)*shadow-(?:primary(?:-raised)?|light|bone-small)(?![\w-])/

const ROOTS = ['src/KCC.Web/Features', 'src/KCC.Web/App_Data/CIRepository'].map((dir) =>
  fileURLToPath(new URL(`../../../../${dir}/`, import.meta.url)),
)
const SCAN = /\.(vue|cshtml|ts|css|xml|json)$/

function* files(dir: string): Generator<string> {
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    const path = join(dir, entry.name)
    if (entry.isDirectory()) yield* files(path)
    else if (SCAN.test(entry.name)) yield path
  }
}

describe('retired design tokens', () => {
  it('appear nowhere in the site source or the persisted CMS content', () => {
    const hits: string[] = []
    for (const root of ROOTS) {
      for (const file of files(root)) {
        readFileSync(file, 'utf8')
          .split('\n')
          .forEach((line, i) => {
            if (RETIRED.test(line)) hits.push(`${relative(root, file)}:${i + 1}: ${line.trim().slice(0, 120)}`)
          })
      }
    }
    expect(hits).toEqual([])
  })
})
