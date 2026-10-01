<script setup lang="ts">
  import { computed } from 'vue'
  import type { VariantSummary } from '~/Types/Recipe'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardRow from '~/Components/Recipe/RecipeCardRow.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'
  import { listTearFor } from '~/Utilities/BrandColor'

  export interface VariantListProps {
    variants: VariantSummary[]
  }

  const props = defineProps<VariantListProps>()
  const rs = useResourceStrings()
  const cards = computed(() => props.variants.map((variant) => ({ key: variant.slug, card: variantToCard(variant, rs) })))
</script>

<template>
  <div class="flex flex-col gap-y-9">
    <RecipeCardRow v-for="(entry, index) in cards" :key="entry.key" :card="entry.card" :tear="listTearFor(index)" />
  </div>
</template>
