<!-- #region StepTimer Component Properties -->
<script lang="ts">
  import { computed, onBeforeUnmount, ref } from 'vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import { remainingSeconds } from './useStepTimers'

  /**
   * Countdown for a duration found in a cook-mode step, set large enough to read from the stove.
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
  <div role="timer" aria-live="off" :aria-label="`${label}: ${formatted}`" class="flex flex-wrap items-center gap-6">
    <div>
      <p class="kcc-kick">{{ label }}</p>
      <p class="flex items-center gap-3">
        <i class="fa-duotone fa-stopwatch text-2xl" aria-hidden="true"></i>
        <span class="kcc-num text-[64px] leading-[72px]" data-test="timer-display">{{ formatted }}</span>
      </p>
    </div>
    <div class="flex items-center gap-3">
      <Button
        variant="ghost"
        size="lg"
        class="kcc-btn--icon shrink-0"
        data-test="timer-toggle"
        :aria-label="running ? t('Pause') : t('StartTimer')"
        @click="toggle"
      >
        <i :class="running ? 'fa-duotone fa-pause' : 'fa-duotone fa-play'" aria-hidden="true"></i>
      </Button>
      <Button
        variant="ghost"
        size="lg"
        class="kcc-btn--icon shrink-0"
        data-test="timer-reset"
        :aria-label="t('Reset')"
        @click="reset"
      >
        <i class="fa-duotone fa-rotate-left" aria-hidden="true"></i>
      </Button>
    </div>
  </div>
</template>
