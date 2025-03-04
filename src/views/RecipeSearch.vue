<script setup lang="ts">
import qs from 'qs'
import SmallHeader from '~/components/SmallHeader.vue'
import RecipeFacet from '~/components/RecipeFacet.vue'

const proteins = ['Beef', 'Chicken', 'Pork', 'Fish']
const filters = qs.parse(window.location.search.slice(1))

console.log(filters)
</script>

<template>
  <SmallHeader dark>
    <template #title>Search Recipes</template>
  </SmallHeader>

  <section>
    <aside>
      <RecipeFacet title="Protein" :options="proteins" :selected="filters.protein as string" />
    </aside>
  </section>
</template>
