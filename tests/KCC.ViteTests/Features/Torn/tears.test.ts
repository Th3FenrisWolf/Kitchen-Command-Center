import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'
import { SHEET_TEARS, TILE_TEARS, tearsCss } from '~/Torn/tears'

const committed = readFileSync(
  fileURLToPath(new URL('../../../../src/KCC.Web/Features/Styles/Torn/Tears.css', import.meta.url)),
  'utf8',
).replaceAll('\r\n', '\n')

describe('Tears.css', () => {
  it('matches the generator (run `yarn tears` after changing tears.ts)', () => {
    expect(committed).toBe(tearsCss())
  })

  it('stays within budget', () => {
    expect(tearsCss().length).toBeLessThan(120_000)
  })
})

describe('tear presets', () => {
  const standard = SHEET_TEARS.filter((p) => p.name !== 'tear-hero')

  it('has six standard sheets, a hero and three tiles', () => {
    expect(standard).toHaveLength(6)
    expect(SHEET_TEARS.map((p) => p.name)).toContain('tear-hero')
    expect(TILE_TEARS).toHaveLength(3)
  })

  it('leaves about one sheet in four uncut', () => {
    expect(standard.filter((p) => p.chamfer === 0).length).toBeGreaterThanOrEqual(1)
  })

  it('never cuts the top-left corner and tilts within a degree', () => {
    for (const p of SHEET_TEARS) {
      expect([1, 2, 3]).toContain(p.corner)
      expect(Math.abs(p.tilt ?? 0)).toBeLessThanOrEqual(1)
    }
  })

  it('uses unique names and seeds', () => {
    const all = [...SHEET_TEARS, ...TILE_TEARS]
    expect(new Set(all.map((p) => p.name)).size).toBe(all.length)
    expect(new Set(all.map((p) => p.seed)).size).toBe(all.length)
  })
})
