<script setup lang="ts">
  import { computed } from 'vue'
  import type { VariantSummary } from '~/Types/Recipe'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import FeaturedRecipeCard from '~/Components/Recipe/FeaturedRecipeCard.vue'
  import { variantToFeatured } from '~/Components/Recipe/recipeCardModel'

  // Detail-side adapter: renders the top variant in the shared featured block ("Top Variant" pill).
  const props = defineProps<{ variant: VariantSummary }>()
  const rs = useResourceStrings()
  const card = computed(() => variantToFeatured(props.variant, rs))
</script>

<template>
  <section class="mt-8">
    <FeaturedRecipeCard :card="card" />
  </section>
</template>
