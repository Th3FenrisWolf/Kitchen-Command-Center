<script setup lang="ts">
  import { computed } from 'vue'
  import type { VariantSummary } from '~/Types/Recipe'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardRow from '~/Components/Recipe/RecipeCardRow.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'

  const props = defineProps<{ variants: VariantSummary[] }>()
  const rs = useResourceStrings()
  const cards = computed(() => props.variants.map((variant) => ({ key: variant.slug, card: variantToCard(variant, rs) })))
</script>

<template>
  <div class="mt-6 flex flex-col gap-3">
    <RecipeCardRow v-for="entry in cards" :key="entry.key" :card="entry.card" />
  </div>
</template>
