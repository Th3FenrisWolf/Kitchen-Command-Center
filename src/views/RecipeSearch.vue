<script setup lang="ts">
import { parseQueryString } from '~/utilities/query-functions'
import SmallHeader from '~/components/SmallHeader.vue'
import RecipeFacet from '~/components/RecipeFacet.vue'

interface RecipeSearchParams {
  protein: string
}

const proteins = ['Beef', 'Chicken', 'Pork', 'Fish']
const filters = parseQueryString<RecipeSearchParams>()
</script>

<template>
  <SmallHeader dark>
    <template #title>Search Recipes</template>
  </SmallHeader>

  <section>
    <aside>
      <RecipeFacet title="Protein" :options="proteins" :selected="filters.protein" />
    </aside>
  </section>
</template>
