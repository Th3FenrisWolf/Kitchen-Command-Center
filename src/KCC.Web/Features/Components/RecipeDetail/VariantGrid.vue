<!-- #region VariantGrid Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import type { VariantSummary } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import RecipeCardView from '~/Components/Recipe/RecipeCard.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'

  /**
   * Card grid of a recipe's variants, closing with a tile that starts a new one.
   */
  export default {
    name: 'VariantGrid',
  }

  export interface VariantGridProps {
    variants: VariantSummary[]
    /**
     * Already carries the parent recipe's identifier as a query string.
     */
    addVariantUrl: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<VariantGridProps>()
  const rs = useResourceStrings()
  const cards = computed(() => props.variants.map((variant) => ({ key: variant.slug, card: variantToCard(variant, rs) })))
</script>

<template>
  <div class="mt-6 -mb-4 grid grid-cols-1 gap-x-4 *:row-span-7 *:mb-4 sm:grid-cols-2 lg:grid-cols-3">
    <RecipeCardView v-for="entry in cards" :key="entry.key" :card="entry.card" />

    <AppLink
      :href="addVariantUrl"
      class="group grid min-h-50 place-items-center content-center gap-3 rounded-3xl border-2 border-dashed border-onyx-light text-onyx-light transition-colors hover:border-onyx hover:text-onyx"
    >
      <span
        class="grid h-14 w-14 place-items-center rounded-full bg-onyx-light text-2xl text-bone transition-colors group-hover:bg-onyx"
      >
        <i class="fa-solid fa-plus" />
      </span>
      <span class="font-casual text-2xl"><ResourceString for="AddVariant" /></span>
    </AppLink>
  </div>
</template>
