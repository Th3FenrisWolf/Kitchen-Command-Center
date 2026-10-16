<script setup lang="ts">
  import { useId } from 'vue'
  import Field from '~/Components/Forms/Field.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { ValueEdit } from './recipeWizardFlow'
  import StepNav from './StepNav.vue'
  import { markId, STEP_PAD } from './stepSheet'
  import ValueMark from './ValueMark.vue'
  import type { WizardValues } from './wizardValues'

  export interface BasicsStepProps {
    values: WizardValues
    tear?: Tear
    canGoBack?: boolean
    canProceed?: boolean
    returnsToReview?: boolean
  }

  const { values, tear, canGoBack = false, canProceed = false, returnsToReview = false } = defineProps<BasicsStepProps>()

  const emit = defineEmits<{ edit: ValueEdit<'recipeName' | 'recipeDescription'>; next: []; back: []; review: [] }>()

  const rs = useResourceStrings()

  const uid = useId()
  const ids = {
    recipeName: `${uid}-recipe-name`,
    recipeDescription: `${uid}-recipe-description`,
  }
</script>

<template>
  <KccSheet crisp wide :tear :pad="STEP_PAD" icon="fa-duotone fa-pen-to-square">
    <template #label><ResourceString for="RecipeBasics" /></template>

    <h2 class="sr-only">{{ rs('RecipeBasics') }}</h2>

    <form class="flex flex-col gap-6" @submit.prevent="emit('next')">
      <Field :control-id="ids.recipeName" required>
        <template #label><ResourceString for="RecipeName" /></template>
        <InputField
          :id="ids.recipeName"
          :model-value="values.recipeName"
          data-testid="wizard-recipe-name"
          required
          type="text"
          :placeholder="rs('RecipeNamePlaceholder')"
          :aria-describedby="values.marks['recipe.name'] && markId(ids.recipeName)"
          @update:model-value="(name: string) => emit('edit', 'recipeName', name)"
        />
        <ValueMark v-if="values.marks['recipe.name']" :id="markId(ids.recipeName)" :mark="values.marks['recipe.name']" />
      </Field>

      <Field :control-id="ids.recipeDescription" required>
        <template #label><ResourceString for="Description" /></template>
        <TextAreaField
          :id="ids.recipeDescription"
          :model-value="values.recipeDescription"
          data-testid="wizard-recipe-description"
          required
          :placeholder="rs('RecipeDescriptionPlaceholder')"
          :aria-describedby="values.marks['recipe.description'] && markId(ids.recipeDescription)"
          @update:model-value="(description) => emit('edit', 'recipeDescription', description)"
        />
        <ValueMark
          v-if="values.marks['recipe.description']"
          :id="markId(ids.recipeDescription)"
          :mark="values.marks['recipe.description']"
        />
      </Field>

      <StepNav :can-go-back :can-proceed :returns-to-review @back="emit('back')" @review="emit('review')" />
    </form>
  </KccSheet>
</template>
