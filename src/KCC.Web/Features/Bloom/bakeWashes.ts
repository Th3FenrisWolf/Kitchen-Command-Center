import { deflateSync } from 'node:zlib'
import { WASH_GRAIN_FILE, WASH_TEXTURE_FOLDER, washBlobFile } from './washTextures.ts'

export const BLOOM_FILTER = {
  bleed: { baseFrequency: [0.012, 0.016], numOctaves: 3, seed: 5, scale: 24, stdDeviation: 12 },
  grain: { baseFrequency: 0.5, numOctaves: 4, seed: 9, k1: 0.62, k2: 0.62 },
} as const

export const BLOB_COUNT = 8
export const BLOB_SHAPE_TEXELS = 32
export const BLOB_MARGIN_TEXELS = 7
export const BLOB_TEXELS = BLOB_SHAPE_TEXELS + 2 * BLOB_MARGIN_TEXELS
export const BLOB_REFERENCE_PX = 320
export const GRAIN_TILE_PX = 64
const RADIUS_RANGE = [32, 68] as const
const SUPERSAMPLE = 4

const BSIZE = 0x100
const BM = 0xff
const PERLIN_N = 0x1000
const RAND_M = 2147483647
const RAND_A = 16807
const RAND_Q = 127773
const RAND_R = 2836

function setupSeed(seed: number): number {
  if (seed <= 0) seed = -(seed % (RAND_M - 1)) + 1
  return Math.min(seed, RAND_M - 1)
}

function random(seed: number): number {
  const result = RAND_A * (seed % RAND_Q) - RAND_R * Math.trunc(seed / RAND_Q)
  return result <= 0 ? result + RAND_M : result
}

interface Stitch {
  width: number
  height: number
  wrapX: number
  wrapY: number
}

// The SVG 1.1 reference implementation of feTurbulence, which every engine follows.
export class Turbulence {
  private readonly lattice = new Int32Array(BSIZE + BSIZE + 2)
  private readonly gradient = Array.from({ length: 4 }, () => new Float64Array((BSIZE + BSIZE + 2) * 2))

  constructor(seed: number) {
    let s = setupSeed(seed)
    let i = 0
    for (let k = 0; k < 4; k++) {
      const g = this.gradient[k]!
      for (i = 0; i < BSIZE; i++) {
        this.lattice[i] = i
        for (let j = 0; j < 2; j++) {
          s = random(s)
          g[i * 2 + j] = ((s % (BSIZE + BSIZE)) - BSIZE) / BSIZE
        }
        const length = Math.sqrt(g[i * 2]! ** 2 + g[i * 2 + 1]! ** 2)
        g[i * 2]! /= length
        g[i * 2 + 1]! /= length
      }
    }
    while (--i) {
      const k = this.lattice[i]!
      s = random(s)
      const j = s % BSIZE
      this.lattice[i] = this.lattice[j]!
      this.lattice[j] = k
    }
    for (i = 0; i < BSIZE + 2; i++) {
      this.lattice[BSIZE + i] = this.lattice[i]!
      for (let k = 0; k < 4; k++)
        for (let j = 0; j < 2; j++) this.gradient[k]![(BSIZE + i) * 2 + j] = this.gradient[k]![i * 2 + j]!
    }
  }

  private noise2(channel: number, x: number, y: number, stitch?: Stitch): number {
    let t = x + PERLIN_N
    let bx0 = Math.trunc(t)
    const rx0 = t - bx0
    let bx1 = bx0 + 1
    t = y + PERLIN_N
    let by0 = Math.trunc(t)
    const ry0 = t - by0
    let by1 = by0 + 1
    if (stitch) {
      if (bx0 >= stitch.wrapX) bx0 -= stitch.width
      if (bx1 >= stitch.wrapX) bx1 -= stitch.width
      if (by0 >= stitch.wrapY) by0 -= stitch.height
      if (by1 >= stitch.wrapY) by1 -= stitch.height
    }
    bx0 &= BM
    bx1 &= BM
    by0 &= BM
    by1 &= BM
    const rx1 = rx0 - 1
    const ry1 = ry0 - 1
    const i = this.lattice[bx0]!
    const j = this.lattice[bx1]!
    const g = this.gradient[channel]!
    const dot = (b: number, rx: number, ry: number) => rx * g[b * 2]! + ry * g[b * 2 + 1]!
    const sx = rx0 * rx0 * (3 - 2 * rx0)
    const sy = ry0 * ry0 * (3 - 2 * ry0)
    const top = dot(this.lattice[i + by0]!, rx0, ry0)
    const a = top + sx * (dot(this.lattice[j + by0]!, rx1, ry0) - top)
    const bottom = dot(this.lattice[i + by1]!, rx0, ry1)
    const b = bottom + sx * (dot(this.lattice[j + by1]!, rx1, ry1) - bottom)
    return a + sy * (b - a)
  }

  /** fractalNoise for one channel at (x, y), mapped to [0, 1] as the filter outputs it. */
  fractal(
    channel: number,
    x: number,
    y: number,
    frequency: readonly [number, number],
    octaves: number,
    tile?: number,
  ): number {
    let [fx, fy] = frequency
    let stitch: Stitch | undefined
    if (tile) {
      const fit = (f: number) => {
        const lo = Math.floor(tile * f) / tile
        const hi = Math.ceil(tile * f) / tile
        return f / lo < hi / f ? lo : hi
      }
      fx = fit(fx)
      fy = fit(fy)
      const width = Math.trunc(tile * fx + 0.5)
      const height = Math.trunc(tile * fy + 0.5)
      stitch = { width, height, wrapX: PERLIN_N + width, wrapY: PERLIN_N + height }
    }
    let sum = 0
    let vx = x * fx
    let vy = y * fy
    let ratio = 1
    for (let octave = 0; octave < octaves; octave++) {
      sum += this.noise2(channel, vx, vy, stitch) / ratio
      vx *= 2
      vy *= 2
      ratio *= 2
      if (stitch) {
        stitch.width += stitch.width
        stitch.wrapX = 2 * stitch.wrapX - PERLIN_N
        stitch.height += stitch.height
        stitch.wrapY = 2 * stitch.wrapY - PERLIN_N
      }
    }
    return Math.min(1, Math.max(0, (sum + 1) / 2))
  }
}

/** The bloom filter's grain at one pixel, as its arithmetic composite applies it to premultiplied colour. */
function grainAt(noise: Turbulence, x: number, y: number) {
  const { baseFrequency, numOctaves, k1, k2 } = BLOOM_FILTER.grain
  const w = [0, 1, 2, 3].map((channel) =>
    noise.fractal(channel, x + 0.5, y + 0.5, [baseFrequency, baseFrequency], numOctaves, GRAIN_TILE_PX),
  )
  const alpha = w[3]!
  return {
    alpha: k1 * alpha + k2,
    tone: (w[0]! + w[1]! + w[2]!) / 3,
    wA: alpha,
  }
}

export interface BakedGrain {
  /** The grain's alpha term, clipped to 1 as a mask must be. */
  alpha: Float64Array
  /** The mean factor the grain's colour term puts on every channel, in linear light. */
  tone: number
  /** The mean of the alpha term before clipping. */
  gain: number
}

export function bakeGrain(): BakedGrain {
  const noise = new Turbulence(BLOOM_FILTER.grain.seed)
  const alpha = new Float64Array(GRAIN_TILE_PX * GRAIN_TILE_PX)
  let tone = 0
  let gain = 0
  for (let y = 0; y < GRAIN_TILE_PX; y++)
    for (let x = 0; x < GRAIN_TILE_PX; x++) {
      const g = grainAt(noise, x, y)
      alpha[y * GRAIN_TILE_PX + x] = Math.min(1, g.alpha)
      gain += g.alpha
      tone += (1 + g.tone * g.wA) / (1 + g.wA)
    }
  return { alpha, tone: tone / alpha.length, gain: gain / alpha.length }
}

export type BlobRadii = [number, number, number, number]

export function blobRadii(index: number): BlobRadii {
  let s = setupSeed(1 + index * 7919)
  const [low, high] = RADIUS_RANGE
  return [0, 1, 2, 3].map(() => {
    s = random(s)
    return low + ((high - low) * (s % 10_000)) / 10_000
  }) as BlobRadii
}

// The border-radius shape `r1 r2 r3 r4 / r4 r1 r2 r3` in a unit box, radii scaled down as CSS does when two
// corners would overlap.
function insideBlob([r1, r2, r3, r4]: BlobRadii, x: number, y: number): boolean {
  const h = [r1, r2, r3, r4].map((r) => r / 100)
  const v = [r4, r1, r2, r3].map((r) => r / 100)
  const f = Math.min(1, 1 / (h[0]! + h[1]!), 1 / (h[3]! + h[2]!), 1 / (v[0]! + v[3]!), 1 / (v[1]! + v[2]!))
  const corners = [
    [h[0]! * f, v[0]! * f, 0, 0],
    [h[1]! * f, v[1]! * f, 1, 0],
    [h[2]! * f, v[2]! * f, 1, 1],
    [h[3]! * f, v[3]! * f, 0, 1],
  ] as const
  if (x < 0 || x > 1 || y < 0 || y > 1) return false
  for (const [rx, ry, cornerX, cornerY] of corners) {
    const cx = cornerX ? 1 - rx : rx
    const cy = cornerY ? 1 - ry : ry
    const beyondX = cornerX ? x > cx : x < cx
    const beyondY = cornerY ? y > cy : y < cy
    if (beyondX && beyondY) return ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1
  }
  return true
}

function gaussianBlur(source: Float64Array, size: number, sigma: number): Float64Array {
  const reach = Math.ceil(sigma * 3)
  const kernel = Array.from({ length: 2 * reach + 1 }, (_, i) => Math.exp(-((i - reach) ** 2) / (2 * sigma * sigma)))
  const total = kernel.reduce((a, b) => a + b, 0)
  const pass = (input: Float64Array, horizontal: boolean) => {
    const output = new Float64Array(input.length)
    for (let y = 0; y < size; y++)
      for (let x = 0; x < size; x++) {
        let sum = 0
        for (let k = -reach; k <= reach; k++) {
          const sx = horizontal ? x + k : x
          const sy = horizontal ? y : y + k
          if (sx >= 0 && sx < size && sy >= 0 && sy < size) sum += input[sy * size + sx]! * kernel[k + reach]!
        }
        output[y * size + x] = sum / total
      }
    return output
  }
  return pass(pass(source, true), false)
}

/**
 * One wash shape: the blob displaced and bled through the filter at BLOB_REFERENCE_PX, with room for its bleed.
 * The inks bleed in their own gradient stops, so the mask reaches full strength at the blob's edge rather than half
 * and fades over twice the bleed beyond it, or the two bleeds would multiply.
 */
export function bakeBlob(index: number): Float64Array {
  const { baseFrequency, numOctaves, seed, scale, stdDeviation } = BLOOM_FILTER.bleed
  const radii = blobRadii(index)
  const pxPerTexel = BLOB_REFERENCE_PX / BLOB_SHAPE_TEXELS
  const spread = 2 * stdDeviation
  const pad = Math.ceil((3 * spread + scale / 2) / pxPerTexel)
  const size = BLOB_SHAPE_TEXELS + 2 * pad
  const noise = new Turbulence(seed)
  const offset = index * BLOB_REFERENCE_PX
  const coverage = (u: number, v: number) => {
    let hits = 0
    for (let sy = 0; sy < SUPERSAMPLE; sy++)
      for (let sx = 0; sx < SUPERSAMPLE; sx++)
        if (
          insideBlob(
            radii,
            (u + (sx + 0.5) / SUPERSAMPLE) / BLOB_SHAPE_TEXELS,
            (v + (sy + 0.5) / SUPERSAMPLE) / BLOB_SHAPE_TEXELS,
          )
        )
          hits++
    return hits / SUPERSAMPLE ** 2
  }
  const displaced = new Float64Array(size * size)
  for (let y = 0; y < size; y++)
    for (let x = 0; x < size; x++) {
      const px = (x - pad + 0.5) * pxPerTexel + offset
      const py = (y - pad + 0.5) * pxPerTexel
      const dx = (scale * (noise.fractal(0, px, py, baseFrequency, numOctaves) - 0.5)) / pxPerTexel
      const dy = (scale * (noise.fractal(1, px, py, baseFrequency, numOctaves) - 0.5)) / pxPerTexel
      displaced[y * size + x] = coverage(x - pad + dx, y - pad + dy)
    }
  const bled = gaussianBlur(displaced, size, spread / pxPerTexel)
  const cropped = new Float64Array(BLOB_TEXELS * BLOB_TEXELS)
  const from = pad - BLOB_MARGIN_TEXELS
  for (let y = 0; y < BLOB_TEXELS; y++)
    for (let x = 0; x < BLOB_TEXELS; x++) cropped[y * BLOB_TEXELS + x] = Math.min(1, 2 * bled[(y + from) * size + x + from]!)
  return cropped
}

const CRC_TABLE = Array.from({ length: 256 }, (_, n) => {
  let c = n
  for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1
  return c >>> 0
})

function crc32(bytes: Uint8Array): number {
  let c = 0xffffffff
  for (const byte of bytes) c = CRC_TABLE[(c ^ byte) & 0xff]! ^ (c >>> 8)
  return (c ^ 0xffffffff) >>> 0
}

function chunk(type: string, data: Uint8Array): Buffer {
  const body = Buffer.concat([Buffer.from(type, 'latin1'), data])
  const out = Buffer.alloc(body.length + 8)
  out.writeUInt32BE(data.length, 0)
  body.copy(out, 4)
  out.writeUInt32BE(crc32(body), body.length + 4)
  return out
}

/** A black PNG whose alpha channel carries the mask, so `mask-mode: match-source` reads it as alpha. */
export function alphaPng(alpha: Float64Array, size: number): Buffer {
  const header = Buffer.alloc(13)
  header.writeUInt32BE(size, 0)
  header.writeUInt32BE(size, 4)
  header[8] = 8
  header[9] = 4
  const rows = Buffer.alloc(size * (1 + size * 2))
  for (let y = 0; y < size; y++) {
    const row = y * (1 + size * 2)
    for (let x = 0; x < size; x++)
      rows[row + 1 + x * 2 + 1] = Math.round(Math.min(1, Math.max(0, alpha[y * size + x]!)) * 255)
  }
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', header),
    chunk('IDAT', deflateSync(rows, { level: 9 })),
    chunk('IEND', new Uint8Array()),
  ])
}

const textureUrl = (file: string) => `url("./${WASH_TEXTURE_FOLDER}/${file}")`

export interface BakedWashes {
  grain: Buffer
  blobs: Buffer[]
  /** The grain's mean colour factor in linear light, folded into each ink. */
  tone: number
  gain: number
}

export function bakeWashes(): BakedWashes {
  const grain = bakeGrain()
  return {
    grain: alphaPng(grain.alpha, GRAIN_TILE_PX),
    blobs: Array.from({ length: BLOB_COUNT }, (_, i) => alphaPng(bakeBlob(i), BLOB_TEXELS)),
    tone: grain.tone,
    gain: grain.gain,
  }
}

export function washTextures(baked: BakedWashes): { file: string; png: Buffer }[] {
  return [{ file: WASH_GRAIN_FILE, png: baked.grain }, ...baked.blobs.map((png, i) => ({ file: washBlobFile(i), png }))]
}

export function washesCss(baked: BakedWashes = bakeWashes()): string {
  const blobs = baked.blobs.map((_, i) => `    --wash-blob-${i}: ${textureUrl(washBlobFile(i))};\n`).join('')
  return (
    `/* GENERATED by \`yarn washes\` from Features/Bloom/bakeWashes.ts, with the textures in ${WASH_TEXTURE_FOLDER}/. Do not ` +
    'edit: bakeWashes.test.ts diffs them against the generator. */\n' +
    '@layer components {\n' +
    '  .kcc-wash {\n' +
    `    --wash-tone: ${baked.tone.toFixed(3)};\n` +
    `    --wash-blob-span: ${(BLOB_TEXELS / BLOB_SHAPE_TEXELS).toFixed(4)};\n` +
    `    --wash-grain: ${textureUrl(WASH_GRAIN_FILE)};\n` +
    blobs +
    '  }\n' +
    '}\n'
  )
}
