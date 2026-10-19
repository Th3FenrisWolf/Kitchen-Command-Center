<script setup lang="ts">
  import { computed } from 'vue'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import Button from '~/Components/Button/Button.vue'
  import RecipeWizard, { type ImportAvailability } from '~/Components/RecipeWizard/RecipeWizard.vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'

  export interface AddVariantViewProps {
    recipeId: string
    recipeName: string
    recipeSlug: string
    resourceStrings?: Record<string, string>
    photoImport?: ImportAvailability
  }

  const props = defineProps<AddVariantViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'AddVariant')

  const recipe = computed(() => ({ key: props.recipeId, name: props.recipeName }))
  const closingLink = computed(() => ({ label: `${rs('BackTo')} ${props.recipeName}`, href: props.recipeSlug }))
</script>

<template>
  <SmallHero dark class="mt-rule-1">
    <template #title>
      <ResourceString for="AddVariantFor" class="mr-2" /><span>{{ recipeName }}</span>
    </template>

    <template #action-button>
      <Button as="a" :href="recipeSlug" variant="ghost">
        <ResourceString for="Cancel" /><i class="fa-duotone fa-xmark" aria-hidden="true"></i>
      </Button>
    </template>
  </SmallHero>

  <RecipeWizard flow="variant" :recipe :resource-strings :closing-link :photo-import />
</template>
