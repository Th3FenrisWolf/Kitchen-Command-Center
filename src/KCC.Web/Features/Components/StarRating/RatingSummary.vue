<!-- #region RatingSummary Component Properties -->
<script lang="ts">
  import StarRating from './StarRating.vue'
  import { formatRating } from './starDisplay'

  /**
   * Read-only stars paired with the numeric average. Callers supply their own review-count
   * and empty-state markup.
   */
  export default {
    name: 'RatingSummary',
  }

  export interface RatingSummaryProps {
    value: number
    /**
     * @default 5
     */
    max?: number
    /**
     * Marks the numeric average for extra emphasis. The identity has one weight, so this only
     * toggles the `data-rating-strong` hook; it no longer changes the rendered markup.
     * @default false
     */
    strong?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { value, max = 5, strong = false } = defineProps<RatingSummaryProps>()
</script>

<template>
  <span class="inline-flex items-center gap-1.5">
    <StarRating :model-value="value" :max readonly />
    <span :data-rating-strong="strong ? '' : undefined" class="kcc-num">{{ formatRating(value) }}</span>
  </span>
</template>
