<script setup lang="ts">
import { ref, onBeforeMount } from 'vue'
import SmallHero from '~/components/SmallHero.vue'
import RecipeFacet from '~/components/RecipeFacet.vue'
import { parseQueryString } from '~/Utilities/QueryFunctions'
import cx from '~/Utilities/CX'
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
      <a href="/recipes/create" class="rounded-3xl bg-bone px-4 py-2 text-xl text-onyx">
        Create Recipe
      </a>
    </template>
  </SmallHero>

  <section class="grid grid-cols-1 gap-4 lg:grid-cols-4">
    <aside class="rounded-3xl bg-base p-4 text-bone">
      <RecipeFacet title="Protein" :options="proteins" :selected="filters.protein" />
    </aside>

    <div class="lg: col-span-3 grid grid-cols-1 gap-4 lg:grid-cols-3">
      <a
        v-for="recipe in recipes.filter((x) => !!x)"
        :href="`/recipes/${recipe.id}`"
        :key="recipe.title"
        :class="
          cx(
            'group/card grid min-h-48 content-center gap-2 rounded-3xl bg-bone p-4 text-center text-onyx shadow-primary transition-all duration-300',
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
      </a>
    </div>
  </section>
</template>
