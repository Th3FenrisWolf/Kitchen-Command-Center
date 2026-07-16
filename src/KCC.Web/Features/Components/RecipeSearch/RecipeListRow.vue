<script setup lang="ts">
  import Link from '~/Components/Links/Link.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import type { RecipeSearchHit } from '~/Types/Recipe'

  defineProps<{ recipe: RecipeSearchHit }>()
</script>

<template>
  <Link
    :href="recipe.slug"
    data-testid="recipe-card"
    :data-recipe-name="recipe.name"
    class="flex items-center gap-4 rounded-3xl bg-bone p-4 text-onyx no-underline shadow-primary transition-shadow hover:shadow-primary-raised"
  >
    <AccentTile :seed="recipe.name" :icon="recipe.icon" class="size-20 flex-none text-3xl" />
    <span class="min-w-0 flex-1">
      <span class="block text-xs font-bold tracking-wide text-onyx-light uppercase">{{ recipe.category }}</span>
      <span class="my-1 flex flex-wrap items-baseline gap-x-2 gap-y-1">
        <span class="font-casual text-2xl">{{ recipe.name }}</span>
        <span class="inline-flex flex-wrap gap-1">
          <Badge v-for="tag in recipe.tags" :key="tag">{{ tag }}</Badge>
        </span>
      </span>
      <span class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-onyx-light">
        <span
          v-if="recipe.reviewCount > 0"
          data-testid="recipe-card-rating"
          :data-average-rating="recipe.averageRating ?? 0"
        >
          <i class="fa-solid fa-star text-peach"></i> {{ formatRating(recipe.averageRating ?? 0) }}
        </span>
        <span><i class="fa-solid fa-layer-group"></i> {{ recipe.variantCount }}</span>
        <span><i class="fa-solid fa-clock"></i> {{ recipe.fastestTime }}m</span>
      </span>
    </span>
    <i class="fa-solid fa-chevron-right flex-none text-sm text-onyx-light"></i>
  </Link>
</template>
