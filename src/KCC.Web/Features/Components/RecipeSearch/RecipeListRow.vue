<!-- #region RecipeListRow Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardRow from '~/Components/Recipe/RecipeCardRow.vue'
  import { hitToCard } from '~/Components/Recipe/recipeCardModel'
  import type { Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { RecipeSearchHit } from '~/Types/Recipe'

  /**
   * Search-side adapter that localizes a hit and hands it to the shared RecipeCardRow.
   */
  export default {
    name: 'RecipeListRow',
  }

  export interface RecipeListRowProps {
    recipe: RecipeSearchHit
    /**
     * Neighbours must never share one: the list passes `(index % 6) + 1`. Unset, the row hashes its name.
     */
    tear?: Exclude<Tear, 'hero'>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<RecipeListRowProps>()
  const rs = useResourceStrings()
  const card = computed(() => hitToCard(props.recipe, rs))
</script>

<template>
  <RecipeCardRow :card="card" :tear="props.tear" />
</template>
