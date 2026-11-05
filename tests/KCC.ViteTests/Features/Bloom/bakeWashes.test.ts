import { readFileSync, readdirSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { inflateSync } from 'node:zlib'
import { describe, expect, it } from 'vitest'
import { BLOB_TEXELS, BLOOM_FILTER, CHALK_TILE_PX, WASH_SPAN, bakeWashes, washTextures, washesCss } from '~/Bloom/bakeWashes'
import { BLOB_COUNT, BLOB_SHAPES } from '~/Bloom/blobShapes'
import { WASH_CHALK_FILE, WASH_TEXTURE_FOLDER, washBlobFile } from '~/Bloom/washTextures'
import { WASH_BLOBS } from '~/Bloom/washes'

const features = (path: string) => fileURLToPath(new URL(`../../../../src/KCC.Web/Features/${path}`, import.meta.url))
const web = (path: string) => readFileSync(features(path), 'utf8').replaceAll('\r\n', '\n')
const textureFolder = `Styles/Torn/${WASH_TEXTURE_FOLDER}`

const baked = bakeWashes()
const css = washesCss(baked)
const LEVEL = 1 / 15

interface Decoded {
  width: number
  height: number
  bitDepth: number
  colourType: number
  transparency: number[]
  alpha: number[]
}

function paeth(a: number, b: number, c: number): number {
  const p = a + b - c
  const pa = Math.abs(p - a)
  const pb = Math.abs(p - b)
  const pc = Math.abs(p - c)
  return pa <= pb && pa <= pc ? a : pb <= pc ? b : c
}

function decode(png: Buffer): Decoded {
  const width = png.readUInt32BE(16)
  const height = png.readUInt32BE(20)
  const bitDepth = png[24]!
  const colourType = png[25]!
  let transparency: number[] = []
  const data: Buffer[] = []
  for (let at = 8; at < png.length;) {
    const length = png.readUInt32BE(at)
    const type = png.toString('latin1', at + 4, at + 8)
    const body = png.subarray(at + 8, at + 8 + length)
    if (type === 'IDAT') data.push(body)
    if (type === 'tRNS') transparency = [...body]
    at += length + 12
  }
  const raw = inflateSync(Buffer.concat(data))
  const stride = Math.ceil((width * bitDepth) / 8)
  const rows: Uint8Array[] = []
  for (let y = 0; y < height; y++) {
    const filter = raw[y * (stride + 1)]!
    const line = Uint8Array.from(raw.subarray(y * (stride + 1) + 1, (y + 1) * (stride + 1)))
    const above = rows[y - 1] ?? new Uint8Array(stride)
    for (let x = 0; x < stride; x++) {
      const left = x > 0 ? line[x - 1]! : 0
      const up = above[x]!
      const upLeft = x > 0 ? above[x - 1]! : 0
      const predicted = [0, left, up, (left + up) >> 1, paeth(left, up, upLeft)][filter]!
      line[x] = (line[x]! + predicted) & 0xff
    }
    rows.push(line)
  }
  const alpha: number[] = []
  for (const line of rows)
    for (let x = 0; x < width; x++) {
      const index = bitDepth === 4 ? (line[x >> 1]! >> (x & 1 ? 0 : 4)) & 0xf : line[x]!
      alpha.push((transparency[index] ?? 255) / 255)
    }
  return { width, height, bitDepth, colourType, transparency, alpha }
}

const mean = (values: readonly number[]) => values.reduce((sum, value) => sum + value, 0) / values.length

describe('Washes.css and its textures', () => {
  it('matches the generator (run `yarn washes` after changing bakeWashes.ts)', () => {
    expect(web('Styles/Torn/Washes.css')).toBe(css)
  })

  it('bakes exactly the chalk tile and one texture per blob', () => {
    expect(washTextures(baked).map(({ file }) => file)).toEqual([
      'wash-chalk.png',
      ...Array.from({ length: 8 }, (_, i) => `wash-blob-${i}.png`),
    ])
  })

  it('holds exactly the textures the generator bakes', () => {
    expect(readdirSync(features(textureFolder)).sort()).toEqual(
      washTextures(baked)
        .map(({ file }) => file)
        .sort(),
    )
  })

  // zlib builds may deflate the same pixels to different bytes, so the textures are compared by their pixels.
  it.each(washTextures(baked).map(({ file, png }) => [file, png] as const))(
    'bakes %s with the pixels on disk',
    (file, png) => {
      expect(decode(readFileSync(features(`${textureFolder}/${file}`))).alpha).toEqual(decode(png).alpha)
    },
  )

  it('stays small: the textures under 16,000 B together, and the stylesheet only names them', () => {
    expect(washTextures(baked).reduce((sum, { png }) => sum + png.length, 0)).toBeLessThan(16_000)
    expect(css.length).toBeLessThan(1_000)
    expect(css).not.toContain('data:')
  })

  it.each(washTextures(baked).map(({ file, png }) => [file, png] as const))(
    'encodes %s as a 4-bit indexed PNG with 16 alpha levels in tRNS',
    (_, png) => {
      const { bitDepth, colourType, transparency } = decode(png)
      expect(bitDepth).toBe(4)
      expect(colourType).toBe(3)
      expect(transparency).toEqual(Array.from({ length: 16 }, (_, k) => k * 17))
    },
  )

  it('bakes one blob for every shape washes.ts can draw', () => {
    expect(BLOB_COUNT).toBe(WASH_BLOBS)
    expect(BLOB_SHAPES).toHaveLength(BLOB_COUNT)
    for (let i = 0; i < BLOB_COUNT; i++)
      expect(css).toContain(`--wash-blob-${i}: url("./${WASH_TEXTURE_FOLDER}/${washBlobFile(i)}");`)
  })

  it('names the chalk tile, the span and the gain, and no tone or grain', () => {
    expect(WASH_SPAN).toBe(1.12)
    expect(css).toContain('--wash-span: 1.12;')
    expect(css).toContain('--wash-gain: 1.5;')
    expect(css).toContain(`--wash-chalk: url("./${WASH_TEXTURE_FOLDER}/${WASH_CHALK_FILE}");`)
    expect(css).not.toContain('--wash-tone')
    expect(css).not.toContain('--wash-grain')
    expect(css).not.toContain('--wash-blob-span')
  })

  it('is imported between the tears and the kit', () => {
    const main = web('Styles/Main.css')
    const order = ['./Torn/Tears.css', './Torn/Washes.css', './Torn/Kit.css'].map((file) => main.indexOf(`'${file}'`))
    expect(order.every((at) => at > -1)).toBe(true)
    expect([...order].sort((a, b) => a - b)).toEqual(order)
  })
})

describe('the baked filter', () => {
  it('carries the approved round’s numbers', () => {
    expect(BLOOM_FILTER).toEqual({
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
    })
  })
})

describe('the chalk tile', () => {
  const { width, height, alpha } = decode(baked.chalk)
  const at = (x: number, y: number) => alpha[y * CHALK_TILE_PX + x]!
  const tileMean = mean(alpha)
  const lines = Array.from({ length: CHALK_TILE_PX }, (_, i) => i)

  it('is 128 texels square, one per CSS px', () => {
    expect(CHALK_TILE_PX).toBe(128)
    expect([width, height]).toEqual([128, 128])
  })

  it('keeps colour in the gaps and peaks at 1, averaging the approved mark', () => {
    expect(Math.min(...alpha)).toBeGreaterThanOrEqual(0.3)
    expect(Math.max(...alpha)).toBeLessThanOrEqual(1)
    expect(Math.abs(tileMean - 0.748)).toBeLessThanOrEqual(0.02)
  })

  it('wraps without a seam', () => {
    const across = mean(lines.flatMap((y) => lines.slice(1).map((x) => Math.abs(at(x, y) - at(x - 1, y)))))
    const down = mean(lines.flatMap((x) => lines.slice(1).map((y) => Math.abs(at(x, y) - at(x, y - 1)))))
    const wrapAcross = mean(lines.map((y) => Math.abs(at(0, y) - at(CHALK_TILE_PX - 1, y))))
    const wrapDown = mean(lines.map((x) => Math.abs(at(x, 0) - at(x, CHALK_TILE_PX - 1))))
    expect(wrapAcross).toBeLessThanOrEqual(1.25 * across)
    expect(wrapDown).toBeLessThanOrEqual(1.25 * down)
  })

  it('levels its rows and columns, so no stripe repeats every tile', () => {
    for (const i of lines) {
      expect(Math.abs(mean(lines.map((x) => at(x, i))) - tileMean)).toBeLessThanOrEqual(0.01)
      expect(Math.abs(mean(lines.map((y) => at(i, y))) - tileMean)).toBeLessThanOrEqual(0.01)
    }
  })
})

describe('the blobs', () => {
  const blobs = baked.blobs.map((png) => decode(png))
  const texel = (fraction: number) => ((fraction + (WASH_SPAN - 1) / 2) / WASH_SPAN) * BLOB_TEXELS

  it('draws each from a seeded ellipse with its centre in [.28, .72] and closest-side radii', () => {
    for (const { cx, cy, rx, ry } of BLOB_SHAPES) {
      for (const centre of [cx, cy]) {
        expect(centre).toBeGreaterThanOrEqual(0.28)
        expect(centre).toBeLessThanOrEqual(0.72)
      }
      expect(rx).toBeCloseTo(Math.min(cx, 1 - cx), 12)
      expect(ry).toBeCloseTo(Math.min(cy, 1 - cy), 12)
    }
  })

  it('bakes eight different shapes', () => {
    expect(new Set(blobs.map(({ alpha }) => alpha.join())).size).toBe(BLOB_COUNT)
  })

  describe.each(Array.from({ length: BLOB_COUNT }, (_, i) => i))('blob %i', (i) => {
    const { width, height, alpha } = blobs[i]!
    const at = (x: number, y: number) => alpha[y * BLOB_TEXELS + x]!
    const { cx, cy } = BLOB_SHAPES[i]!
    const centre = [texel(cx), texel(cy)] as const

    it('is 90 texels square', () => {
      expect(BLOB_TEXELS).toBe(90)
      expect([width, height]).toEqual([90, 90])
    })

    it('is solid at its ellipse’s centre and bare on every edge texel', () => {
      expect(at(Math.floor(centre[0]), Math.floor(centre[1]))).toBe(1)
      for (let k = 0; k < BLOB_TEXELS; k++)
        for (const [x, y] of [
          [k, 0],
          [k, BLOB_TEXELS - 1],
          [0, k],
          [BLOB_TEXELS - 1, k],
        ] as const)
          expect(at(x, y)).toBe(0)
    })

    it('draws no darker ring: along 64 rays from its centre, no step rises over the alpha nearer the centre', () => {
      for (let ray = 0; ray < 64; ray++) {
        const angle = (2 * Math.PI * ray) / 64
        let highest = 0
        for (let t = 0; ; t++) {
          const x = Math.floor(centre[0] + t * Math.cos(angle))
          const y = Math.floor(centre[1] + t * Math.sin(angle))
          if (x < 0 || y < 0 || x >= BLOB_TEXELS || y >= BLOB_TEXELS) break
          if (t > 0) expect(at(x, y) - highest, `ray ${ray}, step ${t}`).toBeLessThanOrEqual(LEVEL + 1e-9)
          highest = Math.max(highest, at(x, y))
        }
      }
    })

    it('centres its ink on the ellipse BLOB_SHAPES names', () => {
      let total = 0
      let sumX = 0
      let sumY = 0
      for (let y = 0; y < BLOB_TEXELS; y++)
        for (let x = 0; x < BLOB_TEXELS; x++) {
          total += at(x, y)
          sumX += at(x, y) * (x + 0.5)
          sumY += at(x, y) * (y + 0.5)
        }
      expect(Math.abs(sumX / total - centre[0]) / BLOB_TEXELS).toBeLessThan(0.05)
      expect(Math.abs(sumY / total - centre[1]) / BLOB_TEXELS).toBeLessThan(0.05)
    })
  })
})
