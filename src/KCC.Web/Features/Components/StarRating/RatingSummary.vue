<script setup lang="ts">
  import StarRating from './StarRating.vue'
  import { formatRating } from './starDisplay'

  export interface RatingSummaryProps {
    value: number
    /**
     * @default 5
     */
    max?: number
  }

  const { value, max = 5 } = defineProps<RatingSummaryProps>()
</script>

<template>
  <span class="inline-flex items-center gap-1.5">
    <StarRating :model-value="value" :max readonly />
    <span class="kcc-num">{{ formatRating(value) }}</span>
  </span>
</template>
