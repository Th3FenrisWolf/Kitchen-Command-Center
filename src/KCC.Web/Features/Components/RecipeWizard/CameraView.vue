<script setup lang="ts">
  import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import { cameraMessage, openCamera, stopCamera, takeStill, type CameraMessage } from './camera'

  type CameraStatus = 'starting' | 'live' | CameraMessage

  const emit = defineEmits<{ capture: [still: Promise<Blob | null>] }>()

  const rs = useResourceStrings()

  const status = ref<CameraStatus>('starting')
  const capturing = ref(false)
  const video = ref<HTMLVideoElement | null>(null)
  let stream: MediaStream | null = null
  let unmounted = false
  let dropStills: (photo: null) => void = () => undefined
  const stillsDropped = new Promise<null>((resolve) => {
    dropStills = resolve
  })

  onMounted(async () => {
    let opened: MediaStream
    try {
      opened = await openCamera()
    } catch (failure) {
      status.value = cameraMessage(failure)
      return
    }

    if (unmounted) {
      stopCamera(opened)
      return
    }

    stream = opened
    status.value = 'live'
    await nextTick()
    if (video.value) {
      video.value.srcObject = opened
      video.value.play().catch(() => undefined)
    }
  })

  onBeforeUnmount(() => {
    unmounted = true
    dropStills(null)
    if (stream) stopCamera(stream)
    stream = null
  })

  async function shoot() {
    if (!stream || !video.value) return

    const still = Promise.race([takeStill(stream, video.value), stillsDropped])
    emit('capture', still)
    capturing.value = true
    try {
      await still
    } finally {
      capturing.value = false
    }
  }
</script>

<template>
  <div class="flex flex-col items-center gap-y-rule-1" :aria-busy="status === 'starting'">
    <video
      v-show="status === 'live'"
      ref="video"
      class="block h-[round(down,60vh,var(--bl))] w-auto max-w-full bg-paper-2 object-contain"
      autoplay
      muted
      playsinline
      data-testid="camera-view"
    ></video>

    <p v-if="status === 'starting'" class="kcc-kick flex items-center gap-2.5">
      <i class="fa-solid fa-circle-notch fa-spin" aria-hidden="true"></i><ResourceString for="CameraStarting" />
    </p>

    <p
      v-else-if="status !== 'live'"
      class="kcc-well kcc-well--warning kcc-kick self-stretch"
      role="status"
      data-testid="camera-message"
    >
      {{ rs(status) }}
    </p>

    <div v-if="status === 'starting' || status === 'live'" class="flex min-h-rule-2 items-start">
      <Button
        variant="ghost"
        size="lg"
        class="kcc-btn--icon"
        :disabled="status !== 'live' || capturing"
        :aria-label="rs('TakePhoto')"
        data-testid="camera-shutter"
        @click="shoot"
      >
        <i class="fa-duotone fa-camera" aria-hidden="true"></i>
      </Button>
    </div>
  </div>
</template>
