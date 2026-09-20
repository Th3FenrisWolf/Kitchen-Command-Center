<!-- #region FeaturedRecipeCard Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import { washFor } from '~/Utilities/BrandColor'
  import type { FeaturedRecipeModel } from '~/Components/Recipe/recipeCardModel'

  /**
   * The library's spotlight above a grid of RecipeCards: one recipe or variant on a hero-torn slip, the
   * recipe's wash pooled in the bottom-right corner away from the kick lines, and a large tile pinned over
   * the top-left corner.
   */
  export default {
    name: 'FeaturedRecipeCard',
  }

  export interface FeaturedRecipeCardProps {
    /**
     * Build with `hitToFeatured` or `variantToFeatured` from recipeCardModel.
     */
    card: FeaturedRecipeModel
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { card } = defineProps<FeaturedRecipeCardProps>()

  const wash = computed(() => ({
    '--c': `var(--color-${washFor(card.seed)})`,
    '--x': '100%',
    '--y': '100%',
    '--w': '34%',
    '--h': '60%',
  }))

  const rating = computed(() =>
    card.rating && card.rating.count > 0
      ? { average: card.rating.average, text: formatRating(card.rating.average), count: card.rating.count }
      : undefined,
  )

  // The model's prose meta (who started it) carries no icon; its numeric chips all carry one.
  const notes = computed(() =>
    [card.eyebrow, ...(card.meta ?? []).filter((item) => !item.icon).map((item) => item.text)].filter(
      (note): note is string => !!note,
    ),
  )
  const chips = computed(() => (card.meta ?? []).filter((item) => item.icon))
</script>

<template>
  <article class="kcc-slip kcc-recipe kcc-tear-hero transition-transform focus-within:-translate-y-1 hover:-translate-y-1">
    <AppLink :href="card.href" v-bind="card.dataAttrs" class="kcc-torn block">
      <div class="kcc-sheet" style="--pad: 48px">
        <span class="kcc-wash" :style="wash" aria-hidden="true"></span>

        <p v-if="notes.length" class="kcc-kick">
          <template v-for="(note, i) in notes" :key="note"><span v-if="i" aria-hidden="true"> · </span>{{ note }}</template>
        </p>

        <h3 class="kcc-h3">{{ card.name }}</h3>

        <p v-if="rating || chips.length" class="kcc-kick flex flex-wrap items-center gap-x-4 text-ink">
          <span
            v-if="rating"
            class="inline-flex items-center gap-2"
            data-testid="recipe-card-rating"
            :data-average-rating="rating.average"
          >
            <i class="fa-duotone fa-star" aria-hidden="true"></i>
            <span class="kcc-num" role="img" :aria-label="`${rating.text} of 5 stars`">{{ rating.text }}</span>
            <span class="text-ink-soft"
              ><span aria-hidden="true">· </span><span class="kcc-num">{{ rating.count }}</span></span
            >
          </span>
          <span v-for="(chip, i) in chips" :key="i" class="kcc-num">
            <i :class="chip.icon" aria-hidden="true"></i> {{ chip.text }}
          </span>
        </p>

        <p v-if="card.description" class="kcc-body">{{ card.description }}</p>

        <div v-if="card.tags?.length" class="kcc-badges mt-6">
          <Badge v-for="tag in card.tags" :key="tag">{{ tag }}</Badge>
        </div>
      </div>
    </AppLink>

    <span class="kcc-label kcc-label--right">
      <i v-if="card.pill.icon" :class="card.pill.icon" aria-hidden="true"></i>{{ card.pill.label }}
    </span>

    <div class="kcc-tilewrap" style="top: -22px; left: 40px">
      <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" large class="size-24" />
      <span class="kcc-tape" aria-hidden="true"></span>
    </div>

    <span class="kcc-tape" aria-hidden="true"></span>
  </article>
</template>
