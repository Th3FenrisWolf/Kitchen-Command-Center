import { deflateSync } from 'node:zlib'
import { BLOB_SHAPES, type BlobCorners, random, setupSeed } from './blobShapes.ts'
import { WASH_CHALK_FILE, WASH_TEXTURE_FOLDER, washBlobFile } from './washTextures.ts'

export const BLOOM_FILTER = {
  lobe: { baseFrequency: [0.012, 0.016], numOctaves: 3, seed: 5, scale: 60 },
  ragged: { baseFrequency: [0.08, 0.08], numOctaves: 2, seed: 3, scale: 12 },
  ends: { baseFrequency: [0.44, 0.004], numOctaves: 1, seed: 11, scale: 10 },
  outline: { stdDeviation: 4 },
  strokes: { baseFrequency: [0.44, 0.1], numOctaves: 2, seed: 11, slope: 5, intercept: -1.6 },
  tooth: { baseFrequency: [0.6, 0.6], numOctaves: 2, seed: 21, slope: 3, intercept: -0.5 },
  gaps: { k2: 1, k3: 0.5 },
  soft: { stdDeviation: 1 },
  profile: [
    [0, 1],
    [0.62, 0.941],
    [0.8, 0.588],
    [0.92, 0.206],
    [1, 0],
  ],
} as const

const BLOB_MARGIN = 0.06
export const WASH_SPAN = 1 + 2 * BLOB_MARGIN
export const WASH_GAIN = BLOOM_FILTER.gaps.k2 + BLOOM_FILTER.gaps.k3
export const BLOB_REFERENCE_PX = 480
export const BLOB_TEXELS = 90
export const CHALK_TILE_PX = 128
const SAMPLES_PER_TEXEL = 6
const ALPHA_LEVELS = 15

const BSIZE = 0x100
const BM = 0xff
const PERLIN_N = 0x1000

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

const clip01 = (value: number) => Math.min(1, Math.max(0, value))

function blur(source: Float64Array, size: number, sigma: number, wrap: boolean): Float64Array {
  const reach = Math.ceil(sigma * 3)
  const kernel = Array.from({ length: 2 * reach + 1 }, (_, i) => Math.exp(-((i - reach) ** 2) / (2 * sigma * sigma)))
  const total = kernel.reduce((a, b) => a + b, 0)
  const pass = (input: Float64Array, horizontal: boolean) => {
    const output = new Float64Array(input.length)
    for (let y = 0; y < size; y++)
      for (let x = 0; x < size; x++) {
        let sum = 0
        for (let k = -reach; k <= reach; k++) {
          let sx = horizontal ? x + k : x
          let sy = horizontal ? y : y + k
          if (wrap) {
            sx = (sx + size) % size
            sy = (sy + size) % size
          } else if (sx < 0 || sx >= size || sy < 0 || sy >= size) continue
          sum += input[sy * size + sx]! * kernel[k + reach]!
        }
        output[y * size + x] = sum / total
      }
    return output
  }
  return pass(pass(source, true), false)
}

const mean = (values: Float64Array, from: number, count: number, step: number) => {
  let sum = 0
  for (let i = 0; i < count; i++) sum += values[from + i * step]!
  return sum / count
}

/**
 * The chalk composite reduced to a mask: the marks with colour in their gaps, blurred and scaled to peak at
 * 1, which `--wash-gain` undoes on the wash's opacity.
 */
export function bakeChalk(): Float64Array {
  const { strokes, tooth, soft, gaps } = BLOOM_FILTER
  const size = CHALK_TILE_PX
  const strokeNoise = new Turbulence(strokes.seed)
  const toothNoise = new Turbulence(tooth.seed)
  const marks = new Float64Array(size * size)
  for (let y = 0; y < size; y++)
    for (let x = 0; x < size; x++) {
      const s = strokeNoise.fractal(0, x + 0.5, y + 0.5, strokes.baseFrequency, strokes.numOctaves, size)
      const t = toothNoise.fractal(0, x + 0.5, y + 0.5, tooth.baseFrequency, tooth.numOctaves, size)
      marks[y * size + x] = clip01(strokes.slope * s + strokes.intercept) * clip01(tooth.slope * t + tooth.intercept)
    }
  const tile = blur(marks, size, soft.stdDeviation, true).map((m) => (gaps.k2 * m + gaps.k3) / WASH_GAIN)
  // A row or column darker than the tile would repeat as a stripe every tile, where the live filter never repeats.
  const tileMean = mean(tile, 0, tile.length, 1)
  for (let y = 0; y < size; y++) {
    const deviation = mean(tile, y * size, size, 1) - tileMean
    for (let x = 0; x < size; x++) tile[y * size + x]! -= deviation
  }
  for (let x = 0; x < size; x++) {
    const deviation = mean(tile, x, size, size) - tileMean
    for (let y = 0; y < size; y++) tile[y * size + x]! -= deviation
  }
  return tile.map(clip01)
}

function profileAt(rho: number): number {
  const stops = BLOOM_FILTER.profile
  for (let i = 1; i < stops.length; i++) {
    const [to, alpha] = stops[i]!
    if (rho <= to) {
      const [from, start] = stops[i - 1]!
      return start + ((alpha - start) * (rho - from)) / (to - from)
    }
  }
  return 0
}

// The border-radius shape `r1 r2 r3 r4 / r4 r1 r2 r3` in a unit box, radii scaled down as CSS does when two
// corners would overlap.
function insideRoundedBox([r1, r2, r3, r4]: BlobCorners, x: number, y: number): boolean {
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

/**
 * One silhouette: the first ink's faded ellipse in its rounded box, displaced and blurred as the filter
 * draws it on a BLOB_REFERENCE_PX wash, over the box `--wash-span` pads the wash to.
 */
export function bakeBlob(index: number): Float64Array {
  const { lobe, ragged, ends, outline } = BLOOM_FILTER
  const shape = BLOB_SHAPES[index]!
  const lobeNoise = new Turbulence(lobe.seed)
  const raggedNoise = new Turbulence(ragged.seed)
  const endsNoise = new Turbulence(ends.seed)
  const size = BLOB_TEXELS * SAMPLES_PER_TEXEL
  const step = (WASH_SPAN * BLOB_REFERENCE_PX) / size
  const origin = -BLOB_MARGIN * BLOB_REFERENCE_PX
  const samples = new Float64Array(size * size)
  for (let j = 0; j < size; j++)
    for (let i = 0; i < size; i++) {
      let x = origin + (i + 0.5) * step
      let y = origin + (j + 0.5) * step
      // Each displacement reads the stage before it, so a lookup runs the filter's chain from its last stage back.
      y += ends.scale * (endsNoise.fractal(1, x + shape.noiseOffset, y, ends.baseFrequency, ends.numOctaves) - 0.5)
      const raggedX = raggedNoise.fractal(0, x + shape.noiseOffset, y, ragged.baseFrequency, ragged.numOctaves)
      const raggedY = raggedNoise.fractal(1, x + shape.noiseOffset, y, ragged.baseFrequency, ragged.numOctaves)
      x += ragged.scale * (raggedX - 0.5)
      y += ragged.scale * (raggedY - 0.5)
      const lobeX = lobeNoise.fractal(0, x + shape.noiseOffset, y, lobe.baseFrequency, lobe.numOctaves)
      const lobeY = lobeNoise.fractal(1, x + shape.noiseOffset, y, lobe.baseFrequency, lobe.numOctaves)
      x = (x + lobe.scale * (lobeX - 0.5)) / BLOB_REFERENCE_PX
      y = (y + lobe.scale * (lobeY - 0.5)) / BLOB_REFERENCE_PX
      samples[j * size + i] = insideRoundedBox(shape.corners, x, y)
        ? profileAt(Math.hypot((x - shape.cx) / shape.rx, (y - shape.cy) / shape.ry))
        : 0
    }
  const blurred = blur(samples, size, outline.stdDeviation / step, false)
  const texels = new Float64Array(BLOB_TEXELS * BLOB_TEXELS)
  for (let j = 0; j < size; j++)
    for (let i = 0; i < size; i++)
      texels[Math.floor(j / SAMPLES_PER_TEXEL) * BLOB_TEXELS + Math.floor(i / SAMPLES_PER_TEXEL)]! +=
        blurred[j * size + i]! / SAMPLES_PER_TEXEL ** 2
  return texels
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

function paeth(a: number, b: number, c: number): number {
  const p = a + b - c
  const pa = Math.abs(p - a)
  const pb = Math.abs(p - b)
  const pc = Math.abs(p - c)
  return pa <= pb && pa <= pc ? a : pb <= pc ? b : c
}

function filterRow(filter: number, row: Uint8Array, above: Uint8Array): Uint8Array {
  return row.map((byte, x) => {
    const left = x > 0 ? row[x - 1]! : 0
    const up = above[x]!
    const upLeft = x > 0 ? above[x - 1]! : 0
    return (byte - [0, left, up, (left + up) >> 1, paeth(left, up, upLeft)][filter]!) & 0xff
  })
}

// The usual adaptive heuristic: the filter whose bytes, read as signed, sum smallest deflates best.
function bestFilteredRow(row: Uint8Array, above: Uint8Array): Uint8Array {
  let best: Uint8Array | undefined
  let bestCost = Infinity
  for (let filter = 0; filter < 5; filter++) {
    const filtered = filterRow(filter, row, above)
    const cost = filtered.reduce((sum, byte) => sum + (byte < 128 ? byte : 256 - byte), 0)
    if (cost < bestCost) {
      bestCost = cost
      best = Uint8Array.of(filter, ...filtered)
    }
  }
  return best!
}

/**
 * A 4-bit indexed PNG whose black palette carries the mask's 16 levels in `tRNS`, so `mask-mode: alpha` reads it.
 */
export function alphaPng(alpha: Float64Array, size: number): Buffer {
  const header = Buffer.alloc(13)
  header.writeUInt32BE(size, 0)
  header.writeUInt32BE(size, 4)
  header[8] = 4
  header[9] = 3
  const stride = Math.ceil(size / 2)
  const rows: Uint8Array[] = []
  let above = new Uint8Array(stride)
  for (let y = 0; y < size; y++) {
    const row = new Uint8Array(stride)
    for (let x = 0; x < size; x++) row[x >> 1]! |= Math.round(clip01(alpha[y * size + x]!) * ALPHA_LEVELS) << (x & 1 ? 0 : 4)
    rows.push(bestFilteredRow(row, above))
    above = row
  }
  const levels = ALPHA_LEVELS + 1
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    chunk('IHDR', header),
    chunk('PLTE', new Uint8Array(3 * levels)),
    chunk(
      'tRNS',
      Uint8Array.from({ length: levels }, (_, k) => Math.round((255 * k) / ALPHA_LEVELS)),
    ),
    chunk('IDAT', deflateSync(Buffer.concat(rows), { level: 9 })),
    chunk('IEND', new Uint8Array()),
  ])
}

const textureUrl = (file: string) => `url("./${WASH_TEXTURE_FOLDER}/${file}")`

export interface BakedWashes {
  chalk: Buffer
  blobs: Buffer[]
}

export function bakeWashes(): BakedWashes {
  return {
    chalk: alphaPng(bakeChalk(), CHALK_TILE_PX),
    blobs: BLOB_SHAPES.map((_, i) => alphaPng(bakeBlob(i), BLOB_TEXELS)),
  }
}

export function washTextures(baked: BakedWashes): { file: string; png: Buffer }[] {
  return [{ file: WASH_CHALK_FILE, png: baked.chalk }, ...baked.blobs.map((png, i) => ({ file: washBlobFile(i), png }))]
}

export function washesCss(baked: BakedWashes = bakeWashes()): string {
  const blobs = baked.blobs.map((_, i) => `    --wash-blob-${i}: ${textureUrl(washBlobFile(i))};\n`).join('')
  return (
    `/* GENERATED by \`yarn washes\` from Features/Bloom/bakeWashes.ts, with the textures in ${WASH_TEXTURE_FOLDER}/. Do not ` +
    'edit: bakeWashes.test.ts diffs them against the generator. */\n' +
    '@layer components {\n' +
    '  .kcc-wash {\n' +
    `    --wash-span: ${WASH_SPAN};\n` +
    `    --wash-gain: ${WASH_GAIN};\n` +
    `    --wash-chalk: ${textureUrl(WASH_CHALK_FILE)};\n` +
    blobs +
    '  }\n' +
    '}\n'
  )
}
