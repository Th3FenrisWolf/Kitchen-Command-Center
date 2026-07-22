<script setup lang="ts">
  import { computed } from 'vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardRow from '~/Components/Recipe/RecipeCardRow.vue'
  import { hitToCard } from '~/Components/Recipe/recipeCardModel'
  import type { RecipeSearchHit } from '~/Types/Recipe'

  // Search-side adapter for the list view; shares the RecipeCardRow layout with the detail variant list.
  const props = defineProps<{ recipe: RecipeSearchHit }>()
  const rs = useResourceStrings()
  const card = computed(() => hitToCard(props.recipe, rs))
</script>

<template>
  <RecipeCardRow :card="card" />
</template>
