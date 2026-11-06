// The client bundle reads these shapes too, so this module imports nothing from Node.

const RAND_M = 2147483647
const RAND_A = 16807
const RAND_Q = 127773
const RAND_R = 2836

/** feTurbulence's Park-Miller source, seeded as the SVG 1.1 reference seeds it. */
export function setupSeed(seed: number): number {
  if (seed <= 0) seed = -(seed % (RAND_M - 1)) + 1
  return Math.min(seed, RAND_M - 1)
}

export function random(seed: number): number {
  const result = RAND_A * (seed % RAND_Q) - RAND_R * Math.trunc(seed / RAND_Q)
  return result <= 0 ? result + RAND_M : result
}

export const BLOB_COUNT = 8

const CENTRE_RANGE = [0.28, 0.72] as const
const CORNER_RANGE = [38, 64] as const
const NOISE_STRIDE_PX = 640

export type BlobCorners = [number, number, number, number]

/** A blob's first-ink ellipse and rounded box, as fractions of the wash's content box. */
export interface BlobShape {
  cx: number
  cy: number
  rx: number
  ry: number
  /** `border-radius: r1 r2 r3 r4 / r4 r1 r2 r3`, in percent. */
  corners: BlobCorners
  /** Where the blob's region of the filter's noise starts, in reference px. */
  noiseOffset: number
}

function blobShape(index: number): BlobShape {
  let s = setupSeed(1 + index * 7919)
  const draw = ([low, high]: readonly [number, number]) => {
    s = random(s)
    return low + ((high - low) * (s % 10_000)) / 10_000
  }
  const cx = draw(CENTRE_RANGE)
  const cy = draw(CENTRE_RANGE)
  const corners = [0, 1, 2, 3].map(() => draw(CORNER_RANGE)) as BlobCorners
  return {
    cx,
    cy,
    rx: Math.min(cx, 1 - cx),
    ry: Math.min(cy, 1 - cy),
    corners,
    noiseOffset: index * NOISE_STRIDE_PX,
  }
}

export const BLOB_SHAPES: readonly BlobShape[] = Array.from({ length: BLOB_COUNT }, (_, i) => blobShape(i))
