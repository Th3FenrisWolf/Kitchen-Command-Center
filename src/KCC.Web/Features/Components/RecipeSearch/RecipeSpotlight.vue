<script setup lang="ts">
  import { computed } from 'vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import FeaturedRecipeCard from '~/Components/Recipe/FeaturedRecipeCard.vue'
  import { hitToFeatured } from '~/Components/Recipe/recipeCardModel'
  import type { RecipeSearchHit } from '~/Types/Recipe'

  // Search-side adapter: renders the top-rated recipe in the shared featured block ("Top Rated" pill).
  const props = defineProps<{ recipe: RecipeSearchHit }>()
  const rs = useResourceStrings()
  const card = computed(() => hitToFeatured(props.recipe, rs))
</script>

<template>
  <div class="mb-6">
    <FeaturedRecipeCard :card="card" />
  </div>
</template>
