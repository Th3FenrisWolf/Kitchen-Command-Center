<script setup lang="ts">
  import type { VariantSummary } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
  import Link from '~/Components/Links/Link.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import RatingSummary from '~/Components/StarRating/RatingSummary.vue'

  defineProps<{
    variant: VariantSummary
  }>()
</script>

<template>
  <section class="mt-8">
    <h2 class="mb-4 flex flex-wrap items-center gap-2">
      <i class="fa-solid fa-star text-lg text-peach"></i>
      <ResourceString for="TopVariant" />
    </h2>

    <Link
      :href="variant.slug"
      class="group relative flex items-start gap-4 rounded-3xl p-4 shadow-primary transition-all hover:-translate-y-1 hover:shadow-primary-raised max-sm:flex-col sm:items-center lg:mx-4"
    >
      <AccentTile :seed="variant.name" :icon="variant.icon" class="h-40 w-full text-6xl lg:w-40" />

      <div class="grow">
        <div v-if="variant.tags.length" class="mb-2 flex flex-wrap gap-2">
          <Badge v-for="tag in variant.tags" :key="tag">{{ tag }}</Badge>
        </div>

        <h3>{{ variant.name }}</h3>
        <p class="mb-2 max-w-[80ch] text-lg">{{ variant.description }}</p>

        <div class="flex flex-wrap items-center gap-x-4 gap-y-1 text-base text-onyx-light">
          <RatingSummary v-if="(variant.reviewCount ?? 0) > 0" :value="variant.averageRating ?? 0" />
          <span><i class="fa-solid fa-clock"></i> {{ variant.totalTime }} <ResourceString for="Min" /></span>
          <span v-if="variant.authorName"><ResourceString for="By" /> {{ variant.authorName }}</span>
        </div>
      </div>

      <span
        class="absolute right-4 bottom-4 grid size-10 place-items-center rounded-full bg-surface-500 text-bone transition-all group-hover:bottom-5 group-hover:-rotate-30"
      >
        <i class="fa-solid fa-arrow-right"></i>
      </span>
    </Link>
  </section>
</template>
