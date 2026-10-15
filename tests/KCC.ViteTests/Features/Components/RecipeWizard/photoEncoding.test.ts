import { afterEach, describe, expect, it, vi } from 'vitest'
import { encodePhoto, type EncodedPhoto } from '~/Components/RecipeWizard/photoEncoding'

const LIMIT = 2_200_000

const jpeg = (size: number) => new Blob([new Uint8Array(size)], { type: 'image/jpeg' })

const original = () => new Blob(['camera original'], { type: 'image/jpeg' })

function stubDecoder(width: number, height: number) {
  const bitmap = { width, height, close: vi.fn() }
  const decode = vi.fn(() => Promise.resolve(bitmap))
  vi.stubGlobal('createImageBitmap', decode)
  return { bitmap, decode }
}

function stubCanvas(sizeAt: (quality: number) => number | null = () => 900_000) {
  const drawings: { width: number; height: number; smoothing: string }[] = []
  const encodings: { type: string; quality: number; width: number; height: number }[] = []
  const context = {
    imageSmoothingQuality: 'low',
    drawImage: vi.fn((_image: unknown, _x: number, _y: number, width: number, height: number) => {
      drawings.push({ width, height, smoothing: context.imageSmoothingQuality })
    }),
  }
  const canvas = {
    width: 300,
    height: 150,
    getContext: vi.fn((_kind: string): typeof context | null => context),
    toBlob: vi.fn((done: (blob: Blob | null) => void, type: string, quality: number) => {
      encodings.push({ type, quality, width: canvas.width, height: canvas.height })
      const size = sizeAt(quality)
      done(size === null ? null : jpeg(size))
    }),
  }
  vi.stubGlobal('document', { createElement: vi.fn(() => canvas) })
  return { canvas, context, drawings, encodings }
}

const sizeOf = (encoded: EncodedPhoto) => {
  if (!('blob' in encoded)) throw new Error('The photo was expected to be kept.')
  return encoded.blob.size
}

const UNSUPPORTED = { refusal: 'UnsupportedFile' }

describe('encodePhoto', () => {
  afterEach(() => {
    vi.unstubAllGlobals()
  })

  describe('target size', () => {
    it.each([
      ['scales a landscape photo down to the image edge, keeping its aspect ratio', 4032, 3024, 2000, 2000, 1500],
      ['scales a portrait photo down to the image edge, keeping its aspect ratio', 3024, 4032, 2000, 1500, 2000],
      ['respects the image edge it is given', 4032, 3024, 1568, 1568, 1176],
      ['never enlarges a photo inside the image edge', 1200, 900, 2000, 1200, 900],
      ['keeps a photo whose long edge is the image edge', 2000, 1000, 2000, 2000, 1000],
    ])('%s: %i x %i at an edge of %i is drawn %i x %i', async (_rule, width, height, edge, drawnWidth, drawnHeight) => {
      stubDecoder(width, height)
      const { drawings, encodings } = stubCanvas()

      await encodePhoto(original(), edge)

      expect(drawings.map(({ width, height }) => [width, height])).toEqual([[drawnWidth, drawnHeight]])
      expect(encodings.map(({ width, height }) => [width, height])).toEqual([[drawnWidth, drawnHeight]])
    })
  })

  it('decodes the file with createImageBitmap and draws it with imageSmoothingQuality set to high', async () => {
    const { bitmap, decode } = stubDecoder(4032, 3024)
    const { context, drawings } = stubCanvas()
    const file = original()

    await encodePhoto(file, 2000)

    expect(decode).toHaveBeenCalledWith(file)
    expect(context.drawImage).toHaveBeenCalledWith(bitmap, 0, 0, 2000, 1500)
    expect(drawings.map(({ smoothing }) => smoothing)).toEqual(['high'])
  })

  describe('quality steps', () => {
    it('writes a JPEG at quality 0.85 when it fits in 2.2 MB', async () => {
      stubDecoder(4032, 3024)
      const { encodings } = stubCanvas(() => LIMIT)

      const encoded = await encodePhoto(original(), 2000)

      expect(encodings.map(({ type, quality }) => [type, quality])).toEqual([['image/jpeg', 0.85]])
      expect(sizeOf(encoded)).toBe(LIMIT)
    })

    it('steps down to quality 0.7 when 0.85 is above 2.2 MB', async () => {
      stubDecoder(4032, 3024)
      const { encodings } = stubCanvas((quality) => (quality === 0.85 ? LIMIT + 1 : 1_400_000))

      const encoded = await encodePhoto(original(), 2000)

      expect(encodings.map(({ quality }) => quality)).toEqual([0.85, 0.7])
      expect(sizeOf(encoded)).toBe(1_400_000)
    })

    it('steps down to quality 0.55 when 0.7 is still above 2.2 MB', async () => {
      stubDecoder(4032, 3024)
      const { encodings } = stubCanvas((quality) => (quality === 0.55 ? 1_900_000 : 3_000_000))

      const encoded = await encodePhoto(original(), 2000)

      expect(encodings.map(({ quality }) => quality)).toEqual([0.85, 0.7, 0.55])
      expect(sizeOf(encoded)).toBe(1_900_000)
    })

    it('refuses a photo still above 2.2 MB at quality 0.55 as UnsupportedFile', async () => {
      stubDecoder(4032, 3024)
      const { encodings } = stubCanvas(() => LIMIT + 1)

      const encoded = await encodePhoto(original(), 2000)

      expect(encodings.map(({ quality }) => quality)).toEqual([0.85, 0.7, 0.55])
      expect(encoded).toEqual(UNSUPPORTED)
    })

    it('moves on to the next quality when the browser cannot write a JPEG at one', async () => {
      stubDecoder(4032, 3024)
      const { encodings } = stubCanvas((quality) => (quality === 0.85 ? null : 1_000_000))

      const encoded = await encodePhoto(original(), 2000)

      expect(encodings.map(({ quality }) => quality)).toEqual([0.85, 0.7])
      expect(sizeOf(encoded)).toBe(1_000_000)
    })
  })

  it('refuses a photo that createImageBitmap cannot decode, such as a HEIC file outside Safari, as UnsupportedFile', async () => {
    vi.stubGlobal(
      'createImageBitmap',
      vi.fn(() => Promise.reject(new DOMException('The source image could not be decoded.', 'InvalidStateError'))),
    )
    const { canvas } = stubCanvas()

    const encoded = await encodePhoto(new Blob(['ftypheic'], { type: 'image/heic' }), 2000)

    expect(encoded).toEqual(UNSUPPORTED)
    expect(canvas.toBlob).not.toHaveBeenCalled()
  })

  it('refuses a photo as UnsupportedFile when the browser gives no drawing context', async () => {
    stubDecoder(4032, 3024)
    const { canvas } = stubCanvas()
    canvas.getContext.mockReturnValue(null)

    expect(await encodePhoto(original(), 2000)).toEqual(UNSUPPORTED)
  })

  it('releases the decoded photo and the canvas pixels once the photo is written', async () => {
    const { bitmap } = stubDecoder(4032, 3024)
    const { canvas } = stubCanvas()

    await encodePhoto(original(), 2000)

    expect(bitmap.close).toHaveBeenCalled()
    expect([canvas.width, canvas.height]).toEqual([0, 0])
  })
})
