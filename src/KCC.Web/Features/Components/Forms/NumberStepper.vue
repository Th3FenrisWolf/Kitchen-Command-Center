<script setup lang="ts">
  import { computed, onUnmounted } from 'vue'

  defineOptions({
    inheritAttrs: false,
  })

  const {
    min,
    max,
    step = 1,
    unit,
    label,
    disabled,
    placeholder,
  } = defineProps<{
    min?: number
    max?: number
    step?: number
    unit?: string
    label?: string
    disabled?: boolean
    placeholder?: string
  }>()

  const model = defineModel<number | undefined>()

  const clamp = (value: number) => {
    let result = value
    if (min !== undefined) result = Math.max(min, result)
    if (max !== undefined) result = Math.min(max, result)
    return result
  }

  const atMin = computed(() => min !== undefined && (model.value ?? min) <= min)
  const atMax = computed(() => max !== undefined && model.value !== undefined && model.value >= max)

  const stepValue = (direction: 1 | -1) => {
    if (disabled) return
    // From an empty field, the first press lands on the minimum (or one step if no min is set).
    model.value = model.value === undefined ? clamp(min ?? step) : clamp(model.value + direction * step)
  }

  const onInput = (event: Event) => {
    const raw = (event.target as HTMLInputElement).value
    const parsed = Number(raw)
    model.value = raw === '' || Number.isNaN(parsed) ? undefined : parsed
  }

  const onBlur = () => {
    // Never empty: a cleared field falls back to the minimum (0 when no min is set).
    if (model.value === undefined) {
      model.value = min ?? 0
      return
    }
    // These fields are integer-valued; round a typed decimal before clamping into range.
    model.value = clamp(Math.round(model.value))
  }

  // Select the current value on focus so typing replaces the default instead of appending to it.
  const onFocus = (event: FocusEvent) => {
    ;(event.target as HTMLInputElement).select()
  }

  const inputWidth = computed(() => {
    // Size the input to exactly its digits (tabular-nums → 1ch per digit) so the unit hugs it.
    const length = model.value === undefined ? 1 : String(model.value).length
    return `${Math.max(1, length)}ch`
  })

  const valueLabel = computed(() => {
    const name = label ?? 'Value'
    return unit ? `${name} in ${unit}` : name
  })
  const decreaseLabel = computed(() => (label ? `Decrease ${label}` : 'Decrease'))
  const increaseLabel = computed(() => (label ? `Increase ${label}` : 'Increase'))

  // --- Press-and-hold auto-repeat -------------------------------------------
  const HOLD_DELAY = 400 // ms held before auto-repeat begins
  const REPEAT_START = 120 // ms between the first repeated steps
  const REPEAT_MIN = 35 // fastest repeat interval
  const REPEAT_ACCEL = 0.88 // interval shrink factor per repeat (acceleration)

  let holdTimer: ReturnType<typeof setTimeout> | undefined
  let holdDirection: 1 | -1 = 1
  let repeatInterval = REPEAT_START

  const canStep = (direction: 1 | -1) => !disabled && (direction === -1 ? !atMin.value : !atMax.value)

  const stopHold = () => {
    if (holdTimer !== undefined) {
      clearTimeout(holdTimer)
      holdTimer = undefined
    }
  }

  const repeatTick = () => {
    if (!canStep(holdDirection)) {
      stopHold()
      return
    }
    stepValue(holdDirection)
    holdTimer = setTimeout(repeatTick, repeatInterval)
    repeatInterval = Math.max(REPEAT_MIN, Math.round(repeatInterval * REPEAT_ACCEL))
  }

  const startHold = (direction: 1 | -1, event: PointerEvent) => {
    if (event.button > 0) return // primary pointer (left-click / touch / pen) only
    stopHold() // cancel any in-flight hold before starting a new one (e.g. overlapping touches)
    if (!canStep(direction)) return
    ;(event.currentTarget as HTMLElement).setPointerCapture(event.pointerId)
    holdDirection = direction
    repeatInterval = REPEAT_START
    stepValue(direction) // immediate first step
    holdTimer = setTimeout(repeatTick, HOLD_DELAY)
  }

  // Keyboard activation fires a click with detail === 0; mouse/touch clicks (detail >= 1) are
  // already handled by the pointer hold above, so we ignore them here to avoid double-stepping.
  const onButtonClick = (direction: 1 | -1, event: MouseEvent) => {
    if (event.detail !== 0) return
    stepValue(direction)
  }

  onUnmounted(stopHold)
</script>

<template>
  <div class="flex items-center gap-2">
    <button
      type="button"
      class="flex size-12 shrink-0 cursor-pointer items-center justify-center rounded-2xl bg-surface-500 text-lg text-bone transition-colors hover:bg-surface-400 active:translate-y-px disabled:cursor-not-allowed disabled:bg-overlay-300"
      :aria-label="decreaseLabel"
      :disabled="disabled || atMin"
      @pointerdown="startHold(-1, $event)"
      @pointerup="stopHold"
      @pointercancel="stopHold"
      @click="onButtonClick(-1, $event)"
    >
      <i class="fa-solid fa-minus" aria-hidden="true"></i>
    </button>

    <label
      class="flex h-12 min-w-24 flex-1 cursor-text items-center justify-center gap-1.5 rounded-2xl bg-bone-dark px-2 outline-focus focus-within:outline-blue"
    >
      <!-- Invisible mirror of the unit: balances the real unit on the right so the number stays centered. -->
      <span v-if="unit" aria-hidden="true" class="invisible shrink-0 text-sm">{{ unit }}</span>
      <input
        :style="{ width: inputWidth }"
        class="shrink-0 bg-transparent p-0 text-center text-lg font-semibold tabular-nums focus-visible:outline-transparent"
        type="number"
        :min="min"
        :max="max"
        :step="step"
        :aria-label="valueLabel"
        :disabled="disabled"
        :placeholder="placeholder"
        :value="model"
        v-bind="$attrs"
        @input="onInput"
        @focus="onFocus"
        @blur="onBlur"
      />
      <span v-if="unit" class="shrink-0 text-sm text-onyx-light">{{ unit }}</span>
    </label>

    <button
      type="button"
      class="flex size-12 shrink-0 cursor-pointer items-center justify-center rounded-2xl bg-surface-500 text-lg text-bone transition-colors hover:bg-surface-400 active:translate-y-px disabled:cursor-not-allowed disabled:bg-overlay-300"
      :aria-label="increaseLabel"
      :disabled="disabled || atMax"
      @pointerdown="startHold(1, $event)"
      @pointerup="stopHold"
      @pointercancel="stopHold"
      @click="onButtonClick(1, $event)"
    >
      <i class="fa-solid fa-plus" aria-hidden="true"></i>
    </button>
  </div>
</template>
