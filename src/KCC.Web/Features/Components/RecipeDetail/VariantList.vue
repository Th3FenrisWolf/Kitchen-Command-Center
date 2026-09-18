<!-- #region VariantList Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import type { VariantSummary } from '~/Types/Recipe'
  import type { Tear } from '~/Components/Sheet/KccSheet.vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardRow from '~/Components/Recipe/RecipeCardRow.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'

  /**
   * Row-per-variant counterpart to VariantGrid.
   */
  export default {
    name: 'VariantList',
  }

  export interface VariantListProps {
    variants: VariantSummary[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<VariantListProps>()
  const rs = useResourceStrings()
  const cards = computed(() => props.variants.map((variant) => ({ key: variant.slug, card: variantToCard(variant, rs) })))

  const tearFor = (index: number) => ((index % 6) + 1) as Exclude<Tear, 'hero'>
</script>

<template>
  <div class="flex flex-col gap-y-9">
    <RecipeCardRow v-for="(entry, index) in cards" :key="entry.key" :card="entry.card" :tear="tearFor(index)" />
  </div>
</template>
