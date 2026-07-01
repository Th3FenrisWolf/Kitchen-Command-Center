<script setup lang="ts">
  import AccentTile from './AccentTile.vue'
  import ComingSoonBadge from '~/Components/ComingSoon/ComingSoonBadge.vue'
  import { ResourceString } from '~/Components/ResourceStrings'

  defineProps<{
    title: string
    seed: string
    description: string
    icon?: string
    image?: string
    authorName?: string
  }>()
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
          <ComingSoonBadge />
        </span>

        <span class="inline-flex items-center gap-1">
          <i class="fa-duotone fa-fire-burner text-maroon"></i>
          <ComingSoonBadge />
        </span>

        <span v-if="authorName" class="text-bone-dark"><ResourceString for="By" /> {{ authorName }}</span>
      </div>

      <p class="mt-4 max-w-[80ch] text-lg">{{ description }}</p>

      <slot name="footer" />
    </div>
  </div>
</template>
