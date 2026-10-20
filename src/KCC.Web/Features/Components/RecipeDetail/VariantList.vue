<script setup lang="ts">
  import type { VariantSummary } from '~/Types/Recipe'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardRow from '~/Components/Recipe/RecipeCardRow.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'
  import { listTearFor } from '~/Utilities/BrandColor'

  export interface VariantListProps {
    variants: VariantSummary[]
    flip?: boolean
  }

  const { flip = false } = defineProps<VariantListProps>()
  const rs = useResourceStrings()
</script>

<template>
  <div class="flex flex-col gap-y-rule-1.5">
    <RecipeCardRow
      v-for="(variant, index) in variants"
      :key="variant.slug"
      :card="variantToCard(variant, rs)"
      :tear="listTearFor(index)"
      :flip
    />
  </div>
</template>
