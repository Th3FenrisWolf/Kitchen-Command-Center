<script setup lang="ts">
  import { computed, onBeforeUnmount, ref } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import { remainingSeconds } from './useStepTimers'

  const props = defineProps<{ seconds: number; label: string }>()
  const t = useResourceStrings()

  const left = ref(props.seconds)
  const running = ref(false)
  let targetEndMs = 0
  let intervalId: ReturnType<typeof setInterval> | null = null

  const formatted = computed(() => {
    const total = Math.max(0, left.value)
    const m = Math.floor(total / 60)
    const s = total % 60
    return `${m}:${String(s).padStart(2, '0')}`
  })

  const clear = () => {
    if (intervalId !== null) {
      clearInterval(intervalId)
      intervalId = null
    }
  }

  const tick = () => {
    left.value = remainingSeconds(targetEndMs)
    if (left.value <= 0) {
      running.value = false
      clear()
    }
  }

  const start = () => {
    targetEndMs = Date.now() + left.value * 1000
    running.value = true
    clear()
    intervalId = setInterval(tick, 250)
  }

  const pause = () => {
    tick()
    running.value = false
    clear()
  }

  const toggle = () => {
    if (left.value <= 0) return
    running.value ? pause() : start()
  }

  const reset = () => {
    clear()
    running.value = false
    left.value = props.seconds
  }

  onBeforeUnmount(clear)
</script>

<template>
  <div
    role="timer"
    aria-live="off"
    :aria-label="`${label}: ${formatted}`"
    class="inline-flex items-center gap-2 rounded-full bg-surface-500 px-3 py-1.5 text-sm text-bone"
  >
    <i class="fa-solid fa-stopwatch text-peach" aria-hidden="true"></i>
    <span class="font-bold">{{ label }}</span>
    <span class="tabular-nums" data-test="timer-display">{{ formatted }}</span>
    <button
      type="button"
      data-test="timer-toggle"
      :aria-label="running ? t('Pause') : t('StartTimer')"
      class="grid h-6 w-6 cursor-pointer place-items-center rounded-full border-none bg-bone text-onyx transition-colors hover:bg-bone-dark"
      @click="toggle"
    >
      <i :class="running ? 'fa-solid fa-pause' : 'fa-solid fa-play'" class="text-[10px]" aria-hidden="true"></i>
    </button>
    <button
      type="button"
      data-test="timer-reset"
      :aria-label="t('Reset')"
      class="grid h-6 w-6 cursor-pointer place-items-center rounded-full border-none bg-transparent text-bone transition-colors hover:text-peach"
      @click="reset"
    >
      <i class="fa-solid fa-rotate-left text-[10px]" aria-hidden="true"></i>
      <span class="sr-only"><ResourceString for="Reset" /></span>
    </button>
  </div>
</template>
