<!-- #region VariantGrid Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import type { VariantSummary } from '~/Types/Recipe'
  import type { Tear } from '~/Components/Sheet/KccSheet.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import RecipeCardView from '~/Components/Recipe/RecipeCard.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'

  /**
   * Card grid of a recipe's variants, closing with a blank slip that starts a new one.
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

  const tearFor = (index: number) => ((index % 6) + 1) as Exclude<Tear, 'hero'>
</script>

<template>
  <div class="grid grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))] gap-x-7 gap-y-9">
    <RecipeCardView v-for="(entry, index) in cards" :key="entry.key" :card="entry.card" :tear="tearFor(index)" />

    <div
      class="kcc-slip transition-transform focus-within:-translate-y-1 hover:-translate-y-1"
      :class="`kcc-tear-${tearFor(cards.length)}`"
    >
      <AppLink :href="addVariantUrl" class="kcc-torn block">
        <div class="kcc-sheet grid justify-items-center gap-6 text-center" style="--pad: 48px">
          <i class="fa-duotone fa-plus text-4xl text-ink-soft" aria-hidden="true"></i>
          <ResourceString for="AddVariant" as="span" class="kcc-h4" />
        </div>
      </AppLink>
    </div>
  </div>
</template>
