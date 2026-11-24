<script setup lang="ts">
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import RecipeWizard, { type ImportAvailability } from '~/Components/RecipeWizard/RecipeWizard.vue'
  import { beforeWizardHydrates } from '~/Components/RecipeWizard/wizardSteps'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'

  defineOptions({ beforeHydrate: beforeWizardHydrates('recipe') })

  export interface CreateRecipeViewProps {
    resourceStrings?: Record<string, string>
    recipesUrl?: string
    photoImport?: ImportAvailability
  }

  const props = defineProps<CreateRecipeViewProps>()

  provideResourceStrings(props.resourceStrings, 'CreateRecipe')
</script>

<template>
  <SmallHero dark class="mt-rule-1">
    <template #title><ResourceString for="CreateRecipe" /></template>
  </SmallHero>

  <RecipeWizard flow="recipe" :resource-strings :recipes-url :photo-import />
</template>
