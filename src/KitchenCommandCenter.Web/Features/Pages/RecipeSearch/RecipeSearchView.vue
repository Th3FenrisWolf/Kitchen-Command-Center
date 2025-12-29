<script setup lang="ts">
import { ref, onBeforeMount } from 'vue'
import SmallHero from '~/components/SmallHero.vue'
import RecipeFacet from '~/components/RecipeFacet.vue'
import { parseQueryString } from '~/utilities/query-functions'
import cx from '~/utilities/cx'
import type { Recipe } from '~/types/recipe'

interface RecipeSearchParams {
  protein: string
}

const proteins = ['Beef', 'Chicken', 'Pork', 'Fish']
const filters = parseQueryString<RecipeSearchParams>()

const recipes = ref<(Recipe | null)[]>([])
onBeforeMount(async () => {
  //recipes.value = await getAllRecipes()
})
</script>

<template>
  <SmallHero dark>
    <template #title>Search Recipes</template>
    <template #actionButton>
      <RouterLink to="/recipes/create" class="bg-bone text-onyx rounded-3xl px-4 py-2 text-xl">
        Create Recipe
      </RouterLink>
    </template>
  </SmallHero>

  <section class="grid grid-cols-1 gap-4 lg:grid-cols-4">
    <aside class="bg-base text-bone rounded-3xl p-4">
      <RecipeFacet title="Protein" :options="proteins" :selected="filters.protein" />
    </aside>

    <div class="lg: col-span-3 grid grid-cols-1 gap-4 lg:grid-cols-3">
      <RouterLink
        v-for="recipe in recipes.filter((x) => !!x)"
        :to="`/recipes/${recipe.id}`"
        :key="recipe.title"
        :class="
          cx(
            'bg-bone text-onyx shadow-primary group/card grid min-h-48 content-center gap-2 rounded-3xl p-4 text-center transition-all duration-300',
            'focus-within:shadow-primary-raised hover:shadow-primary-raised',
          )
        "
      >
        <span class="font-[APCasual] text-3xl">{{ recipe.title }}</span>
        <!-- <img
          loading="lazy"
          height="200"
          width="300"
          class="h-auto w-full rounded-2xl object-cover"
          :src="recipe.image"
          :alt="recipe.title"
        /> -->
        <span>{{ recipe.description }}</span>
      </RouterLink>
    </div>
  </section>
</template>
