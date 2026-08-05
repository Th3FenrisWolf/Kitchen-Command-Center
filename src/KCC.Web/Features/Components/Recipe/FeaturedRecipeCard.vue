<!-- #region FeaturedRecipeCard Component Properties -->
<script lang="ts">
  import type { FeaturedRecipeModel } from '~/Components/Recipe/recipeCardModel'

  export interface FeaturedRecipeCardProps {
    /**
     * The recipe card data to display
     */
    card: FeaturedRecipeModel
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import RatingSummary from '~/Components/StarRating/RatingSummary.vue'

  const { card } = defineProps<FeaturedRecipeCardProps>()
</script>

<template>
  <AppLink
    :href="card.href"
    v-bind="card.dataAttrs"
    class="group relative flex flex-col gap-4 rounded-3xl bg-bone p-4 text-onyx no-underline shadow-primary transition-all hover:-translate-y-1 hover:shadow-primary-raised sm:flex-row sm:items-center sm:gap-6"
  >
    <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" class="h-40 w-full flex-none text-5xl sm:size-40" />

    <div class="min-w-0 flex-1">
      <div class="mb-2 flex flex-wrap items-center gap-2">
        <span class="inline-flex items-center gap-2 rounded-full bg-peach px-2 py-1 text-xs font-bold text-onyx">
          <i v-if="card.pill.icon" :class="card.pill.icon"></i> {{ card.pill.label }}
        </span>
        <Badge v-for="tag in card.tags" :key="tag">{{ tag }}</Badge>
      </div>

      <div class="font-casual text-4xl leading-tight">{{ card.name }}</div>

      <p v-if="card.description" class="mt-2 max-w-[80ch] text-lg">{{ card.description }}</p>

      <div class="mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-base text-onyx-light">
        <span v-if="card.eyebrow" class="text-xs font-bold tracking-wide uppercase">{{ card.eyebrow }}</span>
        <span
          v-if="card.rating && card.rating.count > 0"
          data-testid="recipe-card-rating"
          :data-average-rating="card.rating.average"
        >
          <RatingSummary :value="card.rating.average" />
        </span>
        <span v-for="(item, i) in card.meta" :key="i"><i v-if="item.icon" :class="item.icon"></i> {{ item.text }}</span>
      </div>
    </div>

    <span
      class="absolute right-4 bottom-4 grid size-10 place-items-center rounded-full bg-surface-500 text-bone transition-all group-hover:bottom-5 group-hover:-rotate-30"
    >
      <i class="fa-solid fa-arrow-right"></i>
    </span>
  </AppLink>
</template>
