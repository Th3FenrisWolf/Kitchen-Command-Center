<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
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
    class="grid content-start gap-2 rounded-3xl bg-bone p-4 text-onyx no-underline shadow-primary transition-shadow focus-within:shadow-primary-raised hover:shadow-primary-raised"
  >
    <AccentTile :seed="recipe.name" :icon="recipe.icon" class="h-40 w-full text-5xl" />
    <span class="text-xs font-bold tracking-wide text-onyx-light uppercase">{{ recipe.category }}</span>
    <span class="font-casual text-2xl">{{ recipe.name }}</span>
    <span class="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-onyx-light">
      <span v-if="recipe.reviewCount > 0" data-testid="recipe-card-rating" :data-average-rating="recipe.averageRating ?? 0"
        ><i class="fa-solid fa-star text-peach"></i> {{ formatRating(recipe.averageRating ?? 0) }}</span
      >
      <span v-else class="italic"><ResourceString for="NoRatingsYet" /></span>
      <span><i class="fa-solid fa-layer-group"></i> {{ recipe.variantCount }}</span>
      <span><i class="fa-solid fa-clock"></i> {{ recipe.fastestTime }}m</span>
    </span>
    <span v-if="recipe.tags.length" class="mt-1 flex flex-wrap gap-1">
      <Badge v-for="t in recipe.tags" :key="t">{{ t }}</Badge>
    </span>
  </Link>
</template>
