import { readFileSync, readdirSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { inflateSync } from 'node:zlib'
import { describe, expect, it } from 'vitest'
import {
  BLOB_COUNT,
  BLOB_TEXELS,
  BLOOM_FILTER,
  GRAIN_TILE_PX,
  bakeGrain,
  bakeWashes,
  washTextures,
  washesCss,
} from '~/Bloom/bakeWashes'
import { WASH_TEXTURE_FOLDER } from '~/Bloom/washTextures'
import { WASH_BLOBS, WASH_REACH_PX } from '~/Bloom/washes'

const features = (path: string) => fileURLToPath(new URL(`../../../../src/KCC.Web/Features/${path}`, import.meta.url))
const web = (path: string) => readFileSync(features(path), 'utf8').replaceAll('\r\n', '\n')
const textureFolder = `Styles/Torn/${WASH_TEXTURE_FOLDER}`

const baked = bakeWashes()

function alphaOf(png: Buffer): { size: number; alpha: number[] } {
  const size = png.readUInt32BE(16)
  let at = 8
  const data: Buffer[] = []
  while (at < png.length) {
    const length = png.readUInt32BE(at)
    if (png.toString('latin1', at + 4, at + 8) === 'IDAT') data.push(png.subarray(at + 8, at + 8 + length))
    at += length + 12
  }
  const rows = inflateSync(Buffer.concat(data))
  const alpha: number[] = []
  for (let y = 0; y < size; y++) for (let x = 0; x < size; x++) alpha.push(rows[y * (1 + size * 2) + 1 + x * 2 + 1]! / 255)
  return { size, alpha }
}

describe('Washes.css and its textures', () => {
  it('matches the generator (run `yarn washes` after changing bakeWashes.ts)', () => {
    expect(web('Styles/Torn/Washes.css')).toBe(washesCss())
  })

  it('holds exactly the textures the generator bakes', () => {
    expect(
      readdirSync(features(textureFolder))
        .filter((file) => file.endsWith('.png'))
        .sort(),
    ).toEqual(
      washTextures(baked)
        .map(({ file }) => file)
        .sort(),
    )
  })

  // zlib builds may deflate the same pixels to different bytes, so the textures are compared by their pixels.
  it.each(washTextures(baked).map(({ file, png }) => [file, png] as const))(
    'bakes %s with the pixels on disk',
    (file, png) => {
      expect(alphaOf(readFileSync(features(`${textureFolder}/${file}`)))).toEqual(alphaOf(png))
    },
  )

  it('stays small: the textures under 16 KB together, and the stylesheet only names them', () => {
    expect(washTextures(baked).reduce((sum, { png }) => sum + png.length, 0)).toBeLessThan(16_000)
    expect(washesCss().length).toBeLessThan(1_000)
    expect(washesCss()).not.toContain('data:')
  })

  it('bakes one blob for every shape washes.ts can draw', () => {
    expect(BLOB_COUNT).toBe(WASH_BLOBS)
    for (let i = 0; i < BLOB_COUNT; i++)
      expect(washesCss()).toContain(`--wash-blob-${i}: url("./${WASH_TEXTURE_FOLDER}/wash-blob-${i}.png");`)
    expect(washesCss()).toContain(`--wash-grain: url("./${WASH_TEXTURE_FOLDER}/wash-grain.png");`)
  })

  it('is imported between the tears and the kit', () => {
    const main = web('Styles/Main.css')
    const order = ['./Torn/Tears.css', './Torn/Washes.css', './Torn/Kit.css'].map((file) => main.indexOf(`'${file}'`))
    expect(order.every((at) => at > -1)).toBe(true)
    expect([...order].sort((a, b) => a - b)).toEqual(order)
  })
})

describe('the baked filter', () => {
  it('bleeds by half of --wash-reach and keeps the grain composite contrast.test.ts is calibrated against', () => {
    expect(WASH_REACH_PX).toBe(2 * BLOOM_FILTER.bleed.stdDeviation)
    expect(BLOOM_FILTER.grain.k1).toBe(0.62)
    expect(BLOOM_FILTER.grain.k2).toBe(0.62)
  })

  it('tiles the grain at its own pixel size, its alpha term clipped to 1 and averaging the composite’s gain', () => {
    const { size, alpha } = alphaOf(baked.grain)
    expect(size).toBe(GRAIN_TILE_PX)
    const mean = alpha.reduce((sum, a) => sum + a, 0) / alpha.length
    expect(Math.min(...alpha)).toBeGreaterThan(0.5)
    expect(Math.max(...alpha)).toBe(1)
    expect(mean).toBeGreaterThan(0.88)
    expect(mean).toBeLessThanOrEqual(bakeGrain().gain)
  })

  it('folds the grain’s mean colour factor into a tone below 1', () => {
    expect(baked.tone).toBeGreaterThan(0.8)
    expect(baked.tone).toBeLessThan(1)
    expect(washesCss()).toContain(`--wash-tone: ${baked.tone.toFixed(3)};`)
  })

  it.each(Array.from({ length: BLOB_COUNT }, (_, i) => i))('bakes blob %i solid inside and bled out to its box', (i) => {
    const { size, alpha } = alphaOf(baked.blobs[i]!)
    expect(size).toBe(BLOB_TEXELS)
    const at = (x: number, y: number) => alpha[y * size + x]!
    expect(at(size / 2, size / 2)).toBe(1)
    for (const [x, y] of [
      [0, 0],
      [size - 1, 0],
      [0, size - 1],
      [size - 1, size - 1],
    ] as const)
      expect(at(x, y)).toBeLessThan(0.15)
  })

  it('bakes eight different shapes', () => {
    expect(new Set(baked.blobs.map((png) => png.toString('base64'))).size).toBe(BLOB_COUNT)
  })
})
