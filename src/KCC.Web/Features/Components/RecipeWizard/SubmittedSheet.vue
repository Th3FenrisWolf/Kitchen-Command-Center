<script setup lang="ts">
  import Button from '~/Components/Button/Button.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import type { WizardMode } from './recipeWizardFlow'

  export interface ClosingLink {
    label: string
    href: string
  }

  export interface SubmittedSheetProps {
    mode: WizardMode
    recipesUrl?: string
    closingLink?: ClosingLink
  }

  const { mode, recipesUrl, closingLink } = defineProps<SubmittedSheetProps>()

  const rs = useResourceStrings()
</script>

<template>
  <KccSheet
    class="mt-6"
    wash="green"
    :at="{ x: '100%', y: '100%', w: '30%', h: '40%' }"
    :tear="2"
    tape
    data-testid="wizard-submitted"
  >
    <div class="grid justify-items-center gap-6 text-center">
      <template v-if="mode === 'recipe'">
        <ResourceString for="RecipeSubmitted" as="h2" class="kcc-h3" />
        <ResourceString for="RecipeSubmittedMessage" as="p" class="kcc-body" />
      </template>

      <template v-else>
        <ResourceString for="VariantSubmitted" as="h2" class="kcc-h3" />
        <ResourceString for="VariantSubmittedMessage" as="p" class="kcc-body" />
      </template>

      <Button as="a" :href="closingLink?.href ?? recipesUrl" variant="marker" size="lg" data-testid="wizard-closing-link">
        {{ closingLink?.label ?? rs('BackToRecipes') }}
      </Button>
    </div>
  </KccSheet>
</template>
