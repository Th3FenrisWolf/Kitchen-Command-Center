<script setup lang="ts">
  import StarRating from './StarRating.vue'
  import { formatRating } from './starDisplay'

  // Read-only "stars + numeric average" pair shared by rating displays (recipe hero,
  // featured-variant card). Callers keep their own review-count / empty-state markup.
  withDefaults(
    defineProps<{
      value: number
      max?: number
      strong?: boolean
    }>(),
    { max: 5, strong: false },
  )
</script>

<template>
  <span class="inline-flex items-center gap-1.5">
    <StarRating :model-value="value" :max readonly />
    <span :class="{ 'font-bold': strong }">{{ formatRating(value) }}</span>
  </span>
</template>
