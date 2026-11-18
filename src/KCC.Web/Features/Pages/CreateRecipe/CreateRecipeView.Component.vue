<script setup lang="ts">
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import RecipeWizard, { type ImportAvailability } from '~/Components/RecipeWizard/RecipeWizard.vue'
  import type { DetailsChoices } from '~/Components/RecipeWizard/wizardDetails'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'

  export interface CreateRecipeViewProps {
    resourceStrings?: Record<string, string>
    recipesUrl?: string
    photoImport?: ImportAvailability
    detailsChoices?: DetailsChoices
  }

  const props = defineProps<CreateRecipeViewProps>()

  provideResourceStrings(props.resourceStrings, 'CreateRecipe')
</script>

<template>
  <SmallHero dark class="mt-rule-1">
    <template #title><ResourceString for="CreateRecipe" /></template>
  </SmallHero>

  <RecipeWizard flow="recipe" :resource-strings :recipes-url :photo-import :choices="detailsChoices" />
</template>
