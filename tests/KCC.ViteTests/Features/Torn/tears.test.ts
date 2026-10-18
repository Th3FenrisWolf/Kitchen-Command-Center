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

  it('declares every preset on :root and a class that selects it', () => {
    const css = tearsCss()
    for (const p of [...SHEET_TEARS, ...TILE_TEARS]) {
      expect(css).toContain(`  --${p.name}: polygon(`)
      expect(css).toContain(`  .kcc-${p.name} {\n    --tear: var(--${p.name});`)
    }
  })

  it('writes each sheet tilt to --r and leaves tiles untilted', () => {
    const css = tearsCss()
    for (const p of SHEET_TEARS) expect(css).toContain(`--tear: var(--${p.name});\n    --r: ${p.tilt};`)
    for (const p of TILE_TEARS) expect(css).not.toContain(`--tear: var(--${p.name});\n    --r:`)
  })
})

describe('tear presets', () => {
  const HEROES = ['tear-hero', 'tear-hero-2']
  const standard = SHEET_TEARS.filter((p) => !HEROES.includes(p.name))

  it('has six standard sheets, two hero sheets and three tiles', () => {
    expect(standard).toHaveLength(6)
    expect(SHEET_TEARS.filter((p) => HEROES.includes(p.name)).map((p) => p.name)).toEqual(HEROES)
    expect(TILE_TEARS).toHaveLength(3)
  })

  it('leaves about one sheet in four uncut', () => {
    const uncut = standard.filter((p) => p.chamfer === 0).length
    expect(uncut).toBeGreaterThanOrEqual(1)
    expect(uncut).toBeLessThanOrEqual(2)
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

describe('blank sheets', () => {
  const standard = SHEET_TEARS.filter((p) => /^tear-\d+$/.test(p.name))
  const preset = (name: string) => SHEET_TEARS.find((p) => p.name === name)!

  it('writes every sheet preset its blank after --r', () => {
    const css = tearsCss()
    for (const p of SHEET_TEARS) {
      expect(p.blank, p.name).toBeDefined()
      expect(css).toContain(
        `  .kcc-${p.name} {\n    --tear: var(--${p.name});\n    --r: ${p.tilt};\n    --blank-tear: var(--${p.blank!.tear});\n    --blank-dir: ${p.blank!.dir};\n  }`,
      )
    }
  })

  it('never gives a sheet its own tear for a blank, and names a declared sheet preset', () => {
    const sheetNames = SHEET_TEARS.map((p) => p.name)
    for (const p of SHEET_TEARS) {
      expect(p.blank!.tear).not.toBe(p.name)
      expect(sheetNames).toContain(p.blank!.tear)
    }
  })

  it('maps each standard preset three along the cycle', () => {
    expect(standard.map((p) => [p.name, p.blank!.tear])).toEqual([
      ['tear-1', 'tear-4'],
      ['tear-2', 'tear-5'],
      ['tear-3', 'tear-6'],
      ['tear-4', 'tear-1'],
      ['tear-5', 'tear-2'],
      ['tear-6', 'tear-3'],
    ])
  })

  it('turns each standard blank opposite its tilt, alternating around the cycle', () => {
    for (const p of standard) expect(p.blank!.dir).toBe(-Math.sign(p.tilt!))
    standard.forEach((p, i) => {
      const next = standard[(i + 1) % standard.length]!
      expect(next.blank!.dir, `${p.name} then ${next.name}`).toBe(-p.blank!.dir)
    })
  })

  it('pairs the two hero tears with each other', () => {
    expect(preset('tear-hero').blank).toEqual({ tear: 'tear-hero-2', dir: -1 })
    expect(preset('tear-hero-2').blank).toEqual({ tear: 'tear-hero', dir: 1 })
  })

  it('cuts the second hero at a corner of its own and tilts it within a degree', () => {
    const hero2 = preset('tear-hero-2')
    expect(hero2.pointsPerSide).toBe(160)
    expect(hero2.corner).not.toBe(preset('tear-hero').corner)
    expect(Math.abs(hero2.tilt ?? 0)).toBeLessThanOrEqual(1)
  })

  it('writes no blank for a tile', () => {
    const css = tearsCss()
    for (const p of TILE_TEARS) {
      expect(p.blank).toBeUndefined()
      expect(css).toContain(`  .kcc-${p.name} {\n    --tear: var(--${p.name});\n  }`)
    }
  })
})
