<script setup lang="ts">
  import { computed } from 'vue'
  import AccentTile from './AccentTile.vue'
  import ComingSoonBadge from '~/Components/ComingSoon/ComingSoonBadge.vue'
  import StarRating from '~/Components/StarRating/StarRating.vue'
  import { ResourceString } from '~/Components/ResourceStrings'

  const props = withDefaults(
    defineProps<{
      title: string
      seed: string
      description: string
      icon?: string
      image?: string
      authorName?: string
      averageRating?: number
      reviewCount?: number
      timesCooked?: number
    }>(),
    {},
  )

  const hasRating = computed(() => (props.reviewCount ?? 0) > 0)
</script>

<template>
  <div class="my-4 grid gap-4 rounded-3xl bg-surface-500 p-4 text-bone shadow-primary lg:grid-cols-4">
    <AccentTile :seed :icon :image :alt="title" class="h-40 w-full text-7xl lg:size-full" />

    <div class="lg:col-span-3">
      <p v-if="$slots.eyebrow" class="mb-1 text-sm text-bone-dark uppercase">
        <slot name="eyebrow" />
      </p>

      <h1>{{ title }}</h1>

      <div class="mt-2 flex flex-wrap items-center gap-4">
        <span class="inline-flex items-center gap-1">
          <i class="fa-solid fa-star text-peach"></i>
          <template v-if="reviewCount === undefined && averageRating === undefined">
            <ComingSoonBadge />
          </template>
          <template v-else-if="hasRating">
            <StarRating :model-value="averageRating ?? 0" readonly />
            <span class="font-bold">{{ (averageRating ?? 0).toFixed(1) }}</span>
            <span class="text-bone-dark">({{ reviewCount }} <ResourceString for="Reviews" />)</span>
          </template>
          <template v-else>
            <span class="text-bone-dark"><ResourceString for="NoRatingsYet" /></span>
          </template>
        </span>

        <span v-if="timesCooked === undefined" class="inline-flex items-center gap-1">
          <i class="fa-duotone fa-fire-burner text-maroon"></i>
          <ComingSoonBadge />
        </span>
        <span v-else-if="(timesCooked ?? 0) > 0" data-testid="times-cooked" class="inline-flex items-center gap-1">
          <i class="fa-duotone fa-fire-burner text-maroon"></i>
          <span>{{ timesCooked }} <ResourceString for="TimesCooked" /></span>
        </span>

        <span v-if="authorName" class="text-bone-dark"><ResourceString for="By" /> {{ authorName }}</span>
      </div>

      <p class="mt-4 max-w-[80ch] text-lg">{{ description }}</p>

      <slot name="footer" />
    </div>
  </div>
</template>
