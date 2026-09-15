<!-- #region RangeSlider Component Properties -->
<script lang="ts">
  import { computed } from 'vue'

  /**
   * Dual-thumb slider bounding a numeric range.
   */
  export default {
    name: 'RangeSlider',
  }

  export interface RangeSliderProps {
    /**
     * Bounds of the track, not of the current selection — that lives in the models.
     */
    min: number
    max: number
    /**
     * @default 1
     */
    step?: number
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { min, max, step = 1 } = defineProps<RangeSliderProps>()
  const modelMin = defineModel<number>('modelMin', { required: true })
  const modelMax = defineModel<number>('modelMax', { required: true })

  const pct = (v: number) => ((v - min) / (max - min)) * 100
  const fillStyle = computed(() => `left: ${pct(modelMin.value)}%; right: ${100 - pct(modelMax.value)}%`)

  // Hold the thumbs at least one step apart so they can never cross or stack on
  // the same value. `clamp` also writes the clamped value straight back to the
  // native input: with the one-way :value binding Vue skips the DOM patch when the
  // clamped model value doesn't change, which would otherwise strand a fast-dragged
  // thumb visually past its neighbor (an apparent swap) while the model held firm.
  const clamp = (e: Event, limit: (raw: number) => number): number => {
    const el = e.target as HTMLInputElement
    const value = limit(Number(el.value))
    el.value = String(value)
    return value
  }
  const onMin = (e: Event) => (modelMin.value = clamp(e, (raw) => Math.min(raw, modelMax.value - step)))
  const onMax = (e: Event) => (modelMax.value = clamp(e, (raw) => Math.max(raw, modelMin.value + step)))
</script>

<template>
  <div class="relative mx-0.5 h-5">
    <div class="absolute inset-x-0 top-1.75 h-1.5 rounded-full bg-bone-dark"></div>
    <div class="absolute top-1.75 h-1.5 rounded-full bg-onyx" :style="fillStyle"></div>
    <input
      class="kcc-range"
      type="range"
      :min="min"
      :max="max"
      :step="step ?? 1"
      :value="modelMin"
      aria-label="Minimum time"
      @input="onMin"
    />
    <input
      class="kcc-range"
      type="range"
      :min="min"
      :max="max"
      :step="step ?? 1"
      :value="modelMax"
      aria-label="Maximum time"
      @input="onMax"
    />
  </div>
</template>
