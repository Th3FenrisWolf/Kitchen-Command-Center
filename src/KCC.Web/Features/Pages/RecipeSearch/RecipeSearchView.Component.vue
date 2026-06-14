<script setup lang="ts">
  import { cx } from '~/Utilities/CX'
  import type { RecipeSummary } from '~/Types/Recipe'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'

  const props = defineProps<{
    recipes: RecipeSummary[]
    createRecipeUrl: string
    resourceStrings?: Record<string, string>
  }>()

  useResourceStrings(props.resourceStrings, 'RecipeSearch')
</script>

<template>
  <SmallHero dark>
    <template #title><ResourceString for="SearchRecipes" /></template>
    <template #action-button>
      <a :href="createRecipeUrl" class="rounded-3xl bg-bone px-4 py-2 text-xl text-onyx">
        <ResourceString for="CreateRecipe" />
      </a>
    </template>
  </SmallHero>

  <section class="grid grid-cols-1 gap-4 lg:grid-cols-3">
    <a
      v-for="recipe in recipes"
      :href="recipe.slug"
      :key="recipe.slug"
      :class="
        cx(
          'group/card grid min-h-48 content-center gap-2 rounded-3xl bg-bone p-4 text-center text-onyx shadow-primary transition-all duration-300',
          'focus-within:shadow-primary-raised hover:shadow-primary-raised',
        )
      "
    >
      <img
        v-if="recipe.image"
        loading="lazy"
        class="h-48 w-full rounded-2xl object-cover"
        :src="recipe.image"
        :alt="recipe.name"
      />
      <div v-else class="flex h-48 w-full items-center justify-center rounded-2xl bg-bone-dark" aria-hidden="true">
        <i :class="recipe.icon" class="text-6xl"></i>
      </div>
      <span class="font-casual text-3xl">{{ recipe.name }}</span>
      <span>{{ recipe.description }}</span>
      <span v-if="recipe.variantCount > 0" class="text-sm text-overlay-400">
        {{ recipe.variantCount }} variant{{ recipe.variantCount !== 1 ? 's' : '' }}
      </span>
    </a>
  </section>
</template>
