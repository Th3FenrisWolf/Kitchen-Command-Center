<!-- #region RecipeCard Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import { sheetTearFor } from '~/Utilities/BrandColor'
  import type { Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { RecipeCardModel } from '~/Components/Recipe/recipeCardModel'

  /**
   * Grid slip for a recipe or a variant: the hero stat top-right, the accent tile pinned over the corner.
   */
  export default {
    name: 'RecipeCard',
  }

  export interface RecipeCardProps {
    /**
     * Build with `hitToCard` or `variantToCard` from recipeCardModel.
     */
    card: RecipeCardModel
    /**
     * Neighbours must never share one: a grid passes `(index % 6) + 1`.
     */
    tear?: Exclude<Tear, 'hero'>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { card, tear } = defineProps<RecipeCardProps>()

  const preset = computed(() => tear ?? sheetTearFor(card.seed))
  const rated = computed(() => card.notch.stat === 'rating')
  const reviewCount = computed(() => (rated.value ? card.rating?.count : undefined))

  // Prose opens the meta line; the model's own chips are all numbers, so they close it set in Sono tabular.
  const notes = computed(() =>
    [card.eyebrow, card.rating?.count === 0 ? card.rating.emptyLabel : undefined, card.subtitle].filter(
      (note): note is string => !!note,
    ),
  )
  const chips = computed(() => (card.meta ?? []).filter((item) => item.key !== card.notch.stat))
</script>

<template>
  <article
    class="kcc-slip kcc-recipe transition-transform focus-within:-translate-y-1 hover:-translate-y-1"
    :class="`kcc-tear-${preset}`"
  >
    <AppLink :href="card.href" v-bind="card.dataAttrs" class="kcc-torn block">
      <div class="kcc-sheet">
        <span
          class="kcc-stat"
          :data-testid="rated ? 'recipe-card-rating' : undefined"
          :data-average-rating="rated ? card.rating?.average : undefined"
        >
          <i :class="rated ? 'fa-duotone fa-star' : 'fa-duotone fa-clock'" aria-hidden="true"></i>
          <span class="kcc-num">{{ card.notch.text }}</span>
          <span v-if="reviewCount" class="text-ink-soft">· {{ reviewCount }}</span>
        </span>

        <h3 class="kcc-h4">{{ card.name }}</h3>

        <p v-if="notes.length || chips.length" class="kcc-meta">
          <span v-for="note in notes" :key="note">{{ note }}</span>
          <span v-for="(chip, i) in chips" :key="i" class="kcc-num">
            <i v-if="chip.icon" :class="chip.icon" aria-hidden="true"></i> {{ chip.text }}
          </span>
        </p>

        <p v-if="card.description" class="kcc-body">{{ card.description }}</p>

        <div v-if="card.tags.length" class="kcc-badges mt-6">
          <Badge v-for="tag in card.tags" :key="tag">{{ tag }}</Badge>
        </div>
      </div>
    </AppLink>

    <div class="kcc-tilewrap">
      <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" class="size-14" />
      <span class="kcc-tape" aria-hidden="true"></span>
    </div>
  </article>
</template>
