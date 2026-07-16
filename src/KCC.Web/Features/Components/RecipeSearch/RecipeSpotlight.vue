<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Link from '~/Components/Links/Link.Component.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import type { RecipeSearchHit } from '~/Types/Recipe'

  defineProps<{ recipe: RecipeSearchHit }>()
</script>

<template>
  <Link
    :href="recipe.slug"
    data-testid="recipe-spotlight"
    :data-recipe-name="recipe.name"
    class="relative mb-6 flex items-center gap-6 rounded-3xl bg-bone p-4 text-onyx no-underline shadow-primary transition-shadow hover:shadow-primary-raised"
  >
    <AccentTile :seed="recipe.name" :icon="recipe.icon" class="size-32 flex-none text-5xl" />
    <div class="min-w-0 flex-1">
      <span class="mb-2 inline-flex items-center gap-1.5 rounded-full bg-peach px-2 py-1 text-xs font-bold text-onyx">
        <i class="fa-solid fa-star"></i> <ResourceString for="TopRated" />
      </span>
      <div class="font-casual text-4xl leading-tight">{{ recipe.name }}</div>
      <div class="mt-2.5 flex flex-wrap items-center gap-x-6 gap-y-1 text-base text-onyx-light">
        <span class="text-xs font-bold tracking-wide uppercase">{{ recipe.category }}</span>
        <span
          v-if="recipe.reviewCount > 0"
          data-testid="recipe-card-rating"
          :data-average-rating="recipe.averageRating ?? 0"
        >
          <i class="fa-solid fa-star text-peach"></i> {{ formatRating(recipe.averageRating ?? 0) }}
        </span>
        <span><i class="fa-solid fa-layer-group"></i> {{ recipe.variantCount }} <ResourceString for="Variants" /></span>
        <span><i class="fa-solid fa-clock"></i> {{ recipe.fastestTime }} min</span>
        <span v-if="recipe.startedBy"><ResourceString for="StartedBy" /> {{ recipe.startedBy }}</span>
      </div>
    </div>
    <span class="absolute right-4 bottom-4 grid size-10 place-items-center rounded-full bg-surface-500 text-bone">
      <i class="fa-solid fa-arrow-right"></i>
    </span>
  </Link>
</template>
