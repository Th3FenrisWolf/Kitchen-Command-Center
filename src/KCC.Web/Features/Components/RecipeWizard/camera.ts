import { recipeWizardKeys } from './recipeWizardKeys'

export type CameraMessage = typeof recipeWizardKeys.CameraDenied | typeof recipeWizardKeys.CameraMissing

const REAR_CAMERA: MediaStreamConstraints = {
  audio: false,
  video: { facingMode: { ideal: 'environment' }, width: { ideal: 3840 }, height: { ideal: 2160 } },
}

const FRAME_QUALITY = 0.95

export async function openCamera(): Promise<MediaStream> {
  return navigator.mediaDevices.getUserMedia(REAR_CAMERA)
}

export const cameraMessage = (failure: unknown): CameraMessage =>
  isNamed(failure, 'NotAllowedError') ? recipeWizardKeys.CameraDenied : recipeWizardKeys.CameraMissing

export function stopCamera(stream: MediaStream): void {
  stream.getTracks().forEach((track) => track.stop())
}

export async function takeStill(stream: MediaStream, video: HTMLVideoElement): Promise<Blob | null> {
  const [track] = stream.getVideoTracks()
  const photo = track ? await photoFrom(track) : null
  return photo ?? frameFrom(video)
}

const isNamed = (failure: unknown, name: string) =>
  typeof failure === 'object' && failure !== null && 'name' in failure && failure.name === name

async function photoFrom(track: MediaStreamTrack): Promise<Blob | null> {
  if (typeof ImageCapture !== 'function') return null
  try {
    return await new ImageCapture(track).takePhoto()
  } catch {
    return null
  }
}

async function frameFrom(video: HTMLVideoElement): Promise<Blob | null> {
  const canvas = document.createElement('canvas')
  try {
    canvas.width = video.videoWidth
    canvas.height = video.videoHeight
    canvas.getContext('2d')?.drawImage(video, 0, 0, canvas.width, canvas.height)
    return await new Promise<Blob | null>((resolve) => canvas.toBlob(resolve, 'image/jpeg', FRAME_QUALITY))
  } finally {
    // Safari holds a canvas's pixels until the canvas is collected; a zero size frees them at once.
    canvas.width = 0
    canvas.height = 0
  }
}
