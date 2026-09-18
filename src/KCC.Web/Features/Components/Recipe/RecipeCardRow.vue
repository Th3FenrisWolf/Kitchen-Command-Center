<!-- #region RecipeCardRow Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import { sheetTearFor } from '~/Utilities/BrandColor'
  import type { Tear } from '~/Components/Sheet/KccSheet.vue'
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
    /**
     * Neighbours must never share one: a list passes `(index % 6) + 1`.
     */
    tear?: Exclude<Tear, 'hero'>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { card, tear } = defineProps<RecipeCardRowProps>()

  const preset = computed(() => tear ?? sheetTearFor(card.seed))
  const rating = computed(() =>
    card.rating && card.rating.count > 0
      ? { average: card.rating.average, text: formatRating(card.rating.average) }
      : undefined,
  )

  // Prose opens the meta line; the model's own chips are all numbers, so they close it set in Sono tabular.
  const notes = computed(() =>
    [card.eyebrow, card.rating?.count === 0 ? card.rating.emptyLabel : undefined, card.subtitle].filter(
      (note): note is string => !!note,
    ),
  )
  const chips = computed(() => card.meta ?? [])
</script>

<template>
  <AppLink
    :href="card.href"
    v-bind="card.dataAttrs"
    class="kcc-slip kcc-torn block transition-transform focus-within:-translate-y-1 hover:-translate-y-1"
    :class="`kcc-tear-${preset}`"
    style="--r: 0"
  >
    <div class="kcc-sheet flex items-center gap-4" style="--pad: 16px">
      <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" class="size-14 flex-none" />

      <div class="min-w-0 flex-1">
        <div class="flex flex-wrap items-center gap-x-3">
          <h3 class="kcc-h4">{{ card.name }}</h3>
          <div v-if="card.tags.length" class="kcc-badges">
            <Badge v-for="tag in card.tags" :key="tag">{{ tag }}</Badge>
          </div>
        </div>

        <p v-if="rating || notes.length || chips.length" class="kcc-meta">
          <span v-if="rating" class="kcc-num" data-testid="recipe-card-rating" :data-average-rating="rating.average">
            <i class="fa-duotone fa-star" aria-hidden="true"></i> {{ rating.text }}
          </span>
          <span v-for="note in notes" :key="note">{{ note }}</span>
          <span v-for="(chip, i) in chips" :key="i" class="kcc-num">
            <i v-if="chip.icon" :class="chip.icon" aria-hidden="true"></i> {{ chip.text }}
          </span>
        </p>
      </div>

      <div v-if="card.trailingStat" class="hidden flex-none text-center sm:block">
        <span class="kcc-num block">{{ card.trailingStat.value }}</span>
        <span class="kcc-kick block">{{ card.trailingStat.label }}</span>
      </div>

      <i class="fa-duotone fa-arrow-right flex-none text-ink-soft" aria-hidden="true"></i>
    </div>
  </AppLink>
</template>
