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
  <KccSheet wash-key="wizard-submitted" margin class="mt-rule-1" wash="green" :tear="2" wide data-testid="wizard-submitted">
    <div class="grid justify-items-center gap-y-rule-1 text-center">
      <template v-if="mode === 'recipe'">
        <ResourceString for="RecipeSubmitted" as="h2" class="kcc-heading" />
        <ResourceString for="RecipeSubmittedMessage" as="p" class="kcc-body" />
      </template>

      <template v-else>
        <ResourceString for="VariantSubmitted" as="h2" class="kcc-heading" />
        <ResourceString for="VariantSubmittedMessage" as="p" class="kcc-body" />
      </template>

      <Button as="a" :href="closingLink?.href ?? recipesUrl" variant="marker" size="lg" data-testid="wizard-closing-link">
        {{ closingLink?.label ?? rs('BackToRecipes') }}
      </Button>
    </div>
  </KccSheet>
</template>
