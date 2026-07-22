<script setup lang="ts">
  import Link from '~/Components/Links/Link.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import type { RecipeCardModel } from '~/Components/Recipe/recipeCardModel'

  // Shared grid card for the recipe search results and the recipe-detail variant grid. Purely
  // presentational: callers map their own type into a RecipeCardModel (see recipeCardModel.ts).
  defineProps<{ card: RecipeCardModel }>()
</script>

<template>
  <Link
    :href="card.href"
    v-bind="card.dataAttrs"
    class="grid content-start gap-2 rounded-3xl bg-bone p-4 text-onyx no-underline shadow-primary transition-shadow focus-within:shadow-primary-raised hover:shadow-primary-raised"
  >
    <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" class="h-40 w-full text-5xl" />

    <span v-if="card.eyebrow" class="text-xs font-bold tracking-wide text-onyx-light uppercase">{{ card.eyebrow }}</span>
    <span class="font-casual text-2xl">{{ card.name }}</span>

    <span
      v-if="card.rating || card.meta?.length"
      class="flex flex-wrap items-center gap-x-2 gap-y-1 text-sm text-onyx-light"
    >
      <template v-if="card.rating">
        <span v-if="card.rating.count > 0" data-testid="recipe-card-rating" :data-average-rating="card.rating.average">
          <i class="fa-solid fa-star text-peach"></i> {{ formatRating(card.rating.average) }}
        </span>
        <span v-else class="italic">{{ card.rating.emptyLabel }}</span>
      </template>
      <span v-for="(item, i) in card.meta" :key="i"><i v-if="item.icon" :class="item.icon"></i> {{ item.text }}</span>
    </span>

    <span v-if="card.subtitle" class="text-sm text-onyx-light">{{ card.subtitle }}</span>
    <p v-if="card.description" class="text-sm">{{ card.description }}</p>

    <span v-if="card.tags.length" class="mt-1 flex flex-wrap gap-1">
      <Badge v-for="tag in card.tags" :key="tag">{{ tag }}</Badge>
    </span>
  </Link>
</template>
