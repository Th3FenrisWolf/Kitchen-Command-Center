<script setup lang="ts">
  import Link from '~/Components/Links/Link.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import type { RecipeCardModel } from '~/Components/Recipe/recipeCardModel'

  // Shared list-row layout, the horizontal counterpart to RecipeCard. Same RecipeCardModel drives
  // both, so the search list and the variant list stay visually identical.
  defineProps<{ card: RecipeCardModel }>()
</script>

<template>
  <Link
    :href="card.href"
    v-bind="card.dataAttrs"
    class="flex items-center gap-4 rounded-3xl bg-bone p-4 text-onyx no-underline shadow-primary transition-shadow hover:shadow-primary-raised"
  >
    <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" class="size-20 flex-none text-3xl" />

    <span class="min-w-0 flex-1">
      <span v-if="card.eyebrow" class="block text-xs font-bold tracking-wide text-onyx-light uppercase">{{
        card.eyebrow
      }}</span>
      <span class="my-1 flex flex-wrap items-baseline gap-x-2 gap-y-1">
        <span class="font-casual text-2xl">{{ card.name }}</span>
        <span v-if="card.tags.length" class="inline-flex flex-wrap gap-1">
          <Badge v-for="tag in card.tags" :key="tag">{{ tag }}</Badge>
        </span>
      </span>

      <span
        v-if="card.rating || card.meta?.length"
        class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-onyx-light"
      >
        <template v-if="card.rating">
          <span v-if="card.rating.count > 0" data-testid="recipe-card-rating" :data-average-rating="card.rating.average">
            <i class="fa-solid fa-star text-peach"></i> {{ formatRating(card.rating.average) }}
          </span>
          <span v-else class="italic">{{ card.rating.emptyLabel }}</span>
        </template>
        <span v-for="(item, i) in card.meta" :key="i"><i v-if="item.icon" :class="item.icon"></i> {{ item.text }}</span>
      </span>

      <span v-if="card.subtitle" class="mt-1 block text-sm text-onyx-light">{{ card.subtitle }}</span>
    </span>

    <span v-if="card.trailingStat" class="hidden flex-none text-center text-onyx-light sm:block">
      <span class="block font-casual text-2xl text-onyx">{{ card.trailingStat.value }}</span>
      <span class="text-xs">{{ card.trailingStat.label }}</span>
    </span>
    <i class="fa-solid fa-arrow-right flex-none text-sm text-onyx-light"></i>
  </Link>
</template>
