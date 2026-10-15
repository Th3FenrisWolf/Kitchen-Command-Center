import { recipeWizardKeys } from './recipeWizardKeys'

export const MAX_PHOTO_BYTES = 2_200_000

const JPEG_QUALITIES = [0.85, 0.7, 0.55]

export type EncodedPhoto = { blob: Blob } | { refusal: 'UnsupportedFile' }

const UNSUPPORTED: EncodedPhoto = { refusal: recipeWizardKeys.UnsupportedFile }

export async function encodePhoto(file: Blob, maxImageEdge: number): Promise<EncodedPhoto> {
  const bitmap = await decode(file)
  if (!bitmap) return UNSUPPORTED

  const canvas = document.createElement('canvas')
  try {
    return drawScaled(canvas, bitmap, maxImageEdge) ? await smallestJpeg(canvas) : UNSUPPORTED
  } finally {
    bitmap.close()
    // Safari holds a canvas's pixels until the canvas is collected; a zero size frees them at once.
    canvas.width = 0
    canvas.height = 0
  }
}

async function decode(file: Blob): Promise<ImageBitmap | null> {
  try {
    return await createImageBitmap(file)
  } catch {
    return null
  }
}

function drawScaled(canvas: HTMLCanvasElement, bitmap: ImageBitmap, maxImageEdge: number): boolean {
  const scale = Math.min(1, maxImageEdge / Math.max(bitmap.width, bitmap.height))
  canvas.width = Math.round(bitmap.width * scale)
  canvas.height = Math.round(bitmap.height * scale)

  const context = canvas.getContext('2d')
  if (!context) return false

  context.imageSmoothingQuality = 'high'
  context.drawImage(bitmap, 0, 0, canvas.width, canvas.height)
  return true
}

async function smallestJpeg(canvas: HTMLCanvasElement): Promise<EncodedPhoto> {
  for (const quality of JPEG_QUALITIES) {
    const blob = await jpeg(canvas, quality)
    if (blob && blob.size <= MAX_PHOTO_BYTES) return { blob }
  }
  return UNSUPPORTED
}

const jpeg = (canvas: HTMLCanvasElement, quality: number) =>
  new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', quality))
