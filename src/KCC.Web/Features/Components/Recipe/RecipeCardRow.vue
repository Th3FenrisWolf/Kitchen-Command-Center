<!-- #region RecipeCardRow Component Properties -->
<script lang="ts">
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import type { RecipeCardModel } from '~/Components/Recipe/recipeCardModel'

  /**
   * Horizontal counterpart to RecipeCard, driven by the same model so the search list and the
   * variant list stay visually identical.
   */
  export default {
    name: 'RecipeCardRow',
  }

  export interface RecipeCardRowProps {
    /**
     * Build with `hitToCard` or `variantToCard` from recipeCardModel.
     */
    card: RecipeCardModel
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  defineProps<RecipeCardRowProps>()
</script>

<template>
  <AppLink
    :href="card.href"
    v-bind="card.dataAttrs"
    class="flex items-center gap-4 rounded-3xl bg-paper-2 p-4 text-ink no-underline transition-shadow"
  >
    <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" class="size-20 flex-none text-3xl" />

    <span class="min-w-0 flex-1">
      <span v-if="card.eyebrow" class="block text-xs font-bold tracking-wide text-ink-soft uppercase">{{
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
        class="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-ink-soft"
      >
        <template v-if="card.rating">
          <span v-if="card.rating.count > 0" data-testid="recipe-card-rating" :data-average-rating="card.rating.average">
            <i class="fa-solid fa-star text-rating-ink"></i> {{ formatRating(card.rating.average) }}
          </span>
          <span v-else class="italic">{{ card.rating.emptyLabel }}</span>
        </template>
        <span v-for="(item, i) in card.meta" :key="i"><i v-if="item.icon" :class="item.icon"></i> {{ item.text }}</span>
      </span>

      <span v-if="card.subtitle" class="mt-1 block text-sm text-ink-soft">{{ card.subtitle }}</span>
    </span>

    <span v-if="card.trailingStat" class="hidden flex-none text-center text-ink-soft sm:block">
      <span class="block font-casual text-2xl text-ink">{{ card.trailingStat.value }}</span>
      <span class="text-xs">{{ card.trailingStat.label }}</span>
    </span>
    <i class="fa-solid fa-arrow-right flex-none text-sm text-ink-soft"></i>
  </AppLink>
</template>
