<!-- #region StepTimer Component Properties -->
<script lang="ts">
  import { computed, onBeforeUnmount, ref } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import { remainingSeconds } from './useStepTimers'

  /**
   * Countdown pill for a duration found in a cook-mode step.
   */
  export default {
    name: 'StepTimer',
  }

  export interface StepTimerProps {
    /**
     * Starting duration; also what Reset returns to.
     */
    seconds: number
    /**
     * The duration phrase matched in the instruction, e.g. "10-12 minutes".
     */
    label: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<StepTimerProps>()
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
    if (running.value) pause()
    else start()
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
    class="inline-flex items-center gap-2 rounded-full bg-paper px-3 py-1.5 text-sm text-ink"
  >
    <i class="fa-solid fa-stopwatch text-rating-ink" aria-hidden="true"></i>
    <span class="font-bold">{{ label }}</span>
    <span class="tabular-nums" data-test="timer-display">{{ formatted }}</span>
    <button
      type="button"
      data-test="timer-toggle"
      :aria-label="running ? t('Pause') : t('StartTimer')"
      class="grid h-6 w-6 cursor-pointer place-items-center rounded-full border-none bg-paper-2 text-ink transition-colors hover:bg-desk-2"
      @click="toggle"
    >
      <i :class="running ? 'fa-solid fa-pause' : 'fa-solid fa-play'" class="text-[10px]" aria-hidden="true"></i>
    </button>
    <button
      type="button"
      data-test="timer-reset"
      :aria-label="t('Reset')"
      class="grid h-6 w-6 cursor-pointer place-items-center rounded-full border-none bg-transparent text-ink transition-colors hover:text-rating-ink"
      @click="reset"
    >
      <i class="fa-solid fa-rotate-left text-[10px]" aria-hidden="true"></i>
      <span class="sr-only"><ResourceString for="Reset" /></span>
    </button>
  </div>
</template>
