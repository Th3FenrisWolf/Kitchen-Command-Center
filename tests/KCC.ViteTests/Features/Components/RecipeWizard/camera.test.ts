import { afterEach, describe, expect, it, vi } from 'vitest'
import { cameraMessage, openCamera, stopCamera, takeStill } from '~/Components/RecipeWizard/camera'

const track = (kind: 'video' | 'audio') => ({ kind, stop: vi.fn() })

type FakeTrack = ReturnType<typeof track>

const streamOf = (...tracks: FakeTrack[]) =>
  ({
    getTracks: () => tracks,
    getVideoTracks: () => tracks.filter(({ kind }) => kind === 'video'),
  }) as unknown as MediaStream

const VIDEO = { videoWidth: 3840, videoHeight: 2160 } as unknown as HTMLVideoElement

function stubImageCapture(take: () => Promise<Blob>) {
  const tracks: unknown[] = []
  vi.stubGlobal(
    'ImageCapture',
    class {
      constructor(captured: unknown) {
        tracks.push(captured)
      }

      takePhoto = take
    },
  )
  return tracks
}

function stubCanvas() {
  const encodings: { type: string; width: number; height: number }[] = []
  const context = { drawImage: vi.fn() }
  const canvas = {
    width: 300,
    height: 150,
    getContext: vi.fn(() => context),
    toBlob: vi.fn((done: (blob: Blob | null) => void, type: string) => {
      encodings.push({ type, width: canvas.width, height: canvas.height })
      done(new Blob(['frame'], { type }))
    }),
  }
  vi.stubGlobal('document', { createElement: vi.fn(() => canvas) })
  return { canvas, context, encodings }
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('openCamera', () => {
  it('asks for the rear camera at up to 3840 x 2160, without sound', async () => {
    const stream = streamOf(track('video'))
    const getUserMedia = vi.fn((_constraints: MediaStreamConstraints) => Promise.resolve(stream))
    vi.stubGlobal('navigator', { mediaDevices: { getUserMedia } })

    expect(await openCamera()).toBe(stream)
    expect(getUserMedia).toHaveBeenCalledWith({
      audio: false,
      video: { facingMode: { ideal: 'environment' }, width: { ideal: 3840 }, height: { ideal: 2160 } },
    })
  })

  it('fails, rather than throwing, where the browser offers no camera', async () => {
    vi.stubGlobal('navigator', {})

    await expect(openCamera()).rejects.toBeInstanceOf(TypeError)
  })
})

describe('cameraMessage', () => {
  it('says the camera is blocked when it was refused', () => {
    expect(cameraMessage(new DOMException('Permission denied', 'NotAllowedError'))).toBe('CameraDenied')
  })

  it('reads the refusal by its name, whatever object carries it', () => {
    expect(cameraMessage({ name: 'NotAllowedError' })).toBe('CameraDenied')
  })

  it.each(['NotFoundError', 'NotReadableError', 'OverconstrainedError', 'AbortError', 'SecurityError'])(
    'says no camera was found after a %s',
    (name) => {
      expect(cameraMessage(new DOMException('No camera', name))).toBe('CameraMissing')
    },
  )

  it('says no camera was found after any other failure', () => {
    expect(cameraMessage(new TypeError('getUserMedia is not a function'))).toBe('CameraMissing')
    expect(cameraMessage(undefined)).toBe('CameraMissing')
  })
})

describe('stopCamera', () => {
  it('stops every track of the stream', () => {
    const tracks = [track('video'), track('audio')]

    stopCamera(streamOf(...tracks))

    tracks.forEach(({ stop }) => expect(stop).toHaveBeenCalledOnce())
  })
})

describe('takeStill', () => {
  it('takes the photo with ImageCapture where the browser has it', async () => {
    const photo = new Blob(['photo'], { type: 'image/jpeg' })
    const camera = track('video')
    const captured = stubImageCapture(() => Promise.resolve(photo))
    const { canvas } = stubCanvas()

    expect(await takeStill(streamOf(camera, track('audio')), VIDEO)).toBe(photo)
    expect(captured).toEqual([camera])
    expect(canvas.toBlob).not.toHaveBeenCalled()
  })

  it('draws the frame on show, at its full size, as a JPEG where the browser has no ImageCapture', async () => {
    const { canvas, context, encodings } = stubCanvas()

    const still = await takeStill(streamOf(track('video')), VIDEO)

    expect(context.drawImage).toHaveBeenCalledWith(VIDEO, 0, 0, 3840, 2160)
    expect(encodings).toEqual([{ type: 'image/jpeg', width: 3840, height: 2160 }])
    expect(still?.type).toBe('image/jpeg')
  })

  it('draws the frame on show when ImageCapture cannot take the photo', async () => {
    stubImageCapture(() => Promise.reject(new DOMException('The camera is busy.', 'InvalidStateError')))
    const { context } = stubCanvas()

    const still = await takeStill(streamOf(track('video')), VIDEO)

    expect(context.drawImage).toHaveBeenCalledOnce()
    expect(still?.type).toBe('image/jpeg')
  })

  it('releases the canvas pixels once the frame is written', async () => {
    const { canvas } = stubCanvas()

    await takeStill(streamOf(track('video')), VIDEO)

    expect([canvas.width, canvas.height]).toEqual([0, 0])
  })
})
