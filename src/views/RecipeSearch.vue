<script setup lang="ts">
import SmallHero from '~/components/SmallHero.vue'
import RecipeFacet from '~/components/RecipeFacet.vue'
import { parseQueryString } from '~/utilities/query-functions'
import cx from '~/utilities/cx'

interface RecipeSearchParams {
  protein: string
}

const proteins = ['Beef', 'Chicken', 'Pork', 'Fish']
const filters = parseQueryString<RecipeSearchParams>()

const recipes = [
  {
    title: 'Beef Stew',
    protein: 'Beef',
    image: 'https://picsum.photos/300/200',
    description: 'A hearty beef stew with vegetables and potatoes.',
    guid: '1',
  },
  {
    title: 'Chicken Curry',
    protein: 'Chicken',
    image: 'https://picsum.photos/300/200',
    description: 'A spicy chicken curry with rice.',
    guid: '2',
  },
  {
    title: 'Pork Chops',
    protein: 'Pork',
    image: 'https://picsum.photos/300/200',
    description: 'Grilled pork chops with vegetables.',
    guid: '3',
  },
  {
    title: 'Fish Tacos',
    protein: 'Fish',
    image: 'https://picsum.photos/300/200',
    description: 'Fish tacos with salsa and avocado.',
    guid: '4',
  },
]
</script>

<template>
  <SmallHero dark>
    <template #title>Search Recipes</template>
  </SmallHero>

  <section class="grid grid-cols-1 gap-4 lg:grid-cols-4">
    <aside class="bg-base text-bone rounded-3xl p-4">
      <RecipeFacet title="Protein" :options="proteins" :selected="filters.protein" />
    </aside>

    <div class="lg: col-span-3 grid grid-cols-1 gap-4 lg:grid-cols-3">
      <RouterLink
        :to="`/recipe/${recipe.guid}`"
        v-for="recipe in recipes"
        :key="recipe.title"
        :class="
          cx(
            'bg-bone text-onyx shadow-primary group/card grid min-h-48 content-center gap-2 rounded-3xl p-4 text-center transition-all duration-300',
            'focus-within:shadow-primary-raised hover:shadow-primary-raised',
          )
        "
      >
        <span class="font-[APCasual] text-3xl">{{ recipe.title }}</span>
        <img
          loading="lazy"
          height="200"
          width="300"
          class="h-auto w-full rounded-2xl object-cover"
          :src="recipe.image"
          :alt="recipe.title"
        />
        <span>{{ recipe.description }}</span>
      </RouterLink>
    </div>
  </section>
</template>
