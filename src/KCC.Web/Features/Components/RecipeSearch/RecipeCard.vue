<script setup lang="ts">
  import { computed } from 'vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardView from '~/Components/Recipe/RecipeCard.vue'
  import { hitToCard } from '~/Components/Recipe/recipeCardModel'
  import type { RecipeSearchHit } from '~/Types/Recipe'

  // Search-side adapter: maps a RecipeSearchHit onto the shared RecipeCard so the search grid and the
  // detail variant grid render from one presentation component.
  const props = defineProps<{ recipe: RecipeSearchHit }>()
  const rs = useResourceStrings()
  const card = computed(() => hitToCard(props.recipe, rs))
</script>

<template>
  <RecipeCardView :card="card" />
</template>
