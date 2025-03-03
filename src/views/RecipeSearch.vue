<script setup lang="ts">
import qs from 'qs'
import SmallHeader from '~/components/SmallHeader.vue'

const proteins = ['Beef', 'Chicken', 'Pork', 'Fish']
const filters = qs.parse(window.location.search.slice(1))
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
