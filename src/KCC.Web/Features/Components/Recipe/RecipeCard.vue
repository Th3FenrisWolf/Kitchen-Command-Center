<script setup lang="ts">
  import { computed } from 'vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'
  import KccWashes from '~/Components/Sheet/KccWashes.Component.vue'
  import { sheetTearFor, washFor } from '~/Utilities/BrandColor'
  import type { Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { RecipeCardModel } from '~/Components/Recipe/recipeCardModel'

  export interface RecipeCardProps {
    card: RecipeCardModel
    tear?: Exclude<Tear, 'hero'>
    flip?: boolean
  }

  const { card, tear, flip = false } = defineProps<RecipeCardProps>()

  const preset = computed(() => tear ?? sheetTearFor(card.seed))
  const rated = computed(() => card.notch.stat === 'rating')
  const reviewCount = computed(() => (rated.value ? card.rating?.count : undefined))

  const notes = computed(() =>
    [card.eyebrow, card.rating?.count === 0 ? card.rating.emptyLabel : undefined, card.subtitle].filter(
      (note): note is string => !!note,
    ),
  )
  const chips = computed(() => (card.meta ?? []).filter((item) => item.key !== card.notch.stat))
</script>

<template>
  <article
    class="kcc-slip kcc-slip--fill kcc-recipe kcc-slip--lift"
    :class="[{ 'kcc-slip--flip': flip }, `kcc-tear-${preset}`]"
  >
    <a :href="card.href" v-bind="card.dataAttrs" class="kcc-torn block">
      <div class="kcc-sheet">
        <KccWashes :wash-key="`recipe-card:${card.href}`" size="card" :chosen="washFor(card.seed)" />
        <span
          class="kcc-stat"
          :data-testid="rated ? 'recipe-card-rating' : undefined"
          :data-average-rating="rated ? card.rating?.average : undefined"
        >
          <i :class="rated ? 'fa-duotone fa-star' : 'fa-duotone fa-clock'" aria-hidden="true"></i>
          <span
            class="kcc-num"
            :role="rated ? 'img' : undefined"
            :aria-label="rated ? `${card.notch.text} of 5 stars` : undefined"
            >{{ card.notch.text }}</span
          >
          <span v-if="reviewCount" class="text-ink-soft"><span aria-hidden="true">· </span>{{ reviewCount }}</span>
        </span>

        <h3 class="kcc-title">{{ card.name }}</h3>

        <p v-if="notes.length || chips.length" class="kcc-meta">
          <span v-for="note in notes" :key="note">{{ note }}</span>
          <span v-for="(chip, i) in chips" :key="i" class="kcc-num">
            <i v-if="chip.icon" :class="chip.icon" aria-hidden="true"></i> {{ chip.text }}
          </span>
        </p>

        <p v-if="card.description" class="kcc-body">{{ card.description }}</p>

        <div v-if="card.tags.length" class="kcc-badges mt-rule-1">
          <span v-for="tag in card.tags" :key="tag" class="kcc-badge">{{ tag }}</span>
        </div>
      </div>
    </a>

    <div class="kcc-tilewrap">
      <AccentTile :seed="card.seed" :icon="card.icon" :image="card.image" class="size-14" />
      <span class="kcc-tape" aria-hidden="true"></span>
    </div>

    <span class="kcc-blank" aria-hidden="true"><span></span></span>
  </article>
</template>
