<script setup lang="ts">
  import { useId } from 'vue'
  import Field from '~/Components/Forms/Field.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import NumberStepper from '~/Components/Forms/NumberStepper.vue'
  import TextAreaField from '~/Components/Forms/TextAreaField.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { ValueEdit, WizardMode } from './recipeWizardFlow'
  import StepNav from './StepNav.vue'
  import { markId, STEP_PAD } from './stepSheet'
  import ValueMark from './ValueMark.vue'
  import { fill } from './wizardText'
  import type { WizardValues } from './wizardValues'

  export interface VariantStepProps {
    mode: WizardMode
    values: WizardValues
    tear?: Tear
    canGoBack?: boolean
    canProceed?: boolean
    returnsToReview?: boolean
  }

  const {
    mode,
    values,
    tear,
    canGoBack = false,
    canProceed = false,
    returnsToReview = false,
  } = defineProps<VariantStepProps>()

  const emit = defineEmits<{
    edit: ValueEdit<'variantName' | 'variantDescription' | 'prepTime' | 'cookTime' | 'servings'>
    next: []
    back: []
    review: []
  }>()

  const rs = useResourceStrings()

  const uid = useId()
  const ids = {
    variantName: `${uid}-variant-name`,
    variantDescription: `${uid}-variant-description`,
    prepTime: `${uid}-prep-time`,
    cookTime: `${uid}-cook-time`,
    servings: `${uid}-servings`,
  }

  const stepperLabels = (name: string, unit?: string) => ({
    label: name,
    valueLabel: unit ? fill(rs('ValueInUnit'), name, unit) : name,
    decreaseLabel: fill(rs('DecreaseLabel'), name),
    increaseLabel: fill(rs('IncreaseLabel'), name),
  })
</script>

<template>
  <KccSheet
    crisp
    :tear
    :pad="STEP_PAD"
    :icon="mode === 'recipe' ? 'fa-duotone fa-layer-group' : 'fa-duotone fa-pen-to-square'"
  >
    <template #label>
      <ResourceString v-if="mode === 'recipe'" for="FirstVariant" />
      <ResourceString v-else for="VariantInfo" />
    </template>

    <h2 class="sr-only">{{ mode === 'recipe' ? rs('FirstVariant') : rs('VariantInfo') }}</h2>

    <form class="flex flex-col gap-6" @submit.prevent="emit('next')">
      <ResourceString v-if="mode === 'recipe'" for="FirstVariantHint" as="p" class="kcc-body text-ink-soft" />

      <Field :control-id="ids.variantName" required>
        <template #label><ResourceString for="VariantName" /></template>
        <InputField
          :id="ids.variantName"
          :model-value="values.variantName"
          data-testid="wizard-variant-name"
          required
          type="text"
          :placeholder="rs('VariantNamePlaceholder')"
          :aria-describedby="values.marks['variant.name'] && markId(ids.variantName)"
          @update:model-value="(name: string) => emit('edit', 'variantName', name)"
        />
        <ValueMark v-if="values.marks['variant.name']" :id="markId(ids.variantName)" :mark="values.marks['variant.name']" />
      </Field>

      <Field :control-id="ids.variantDescription">
        <template #label><ResourceString for="Description" /></template>
        <TextAreaField
          :id="ids.variantDescription"
          :model-value="values.variantDescription"
          data-testid="wizard-variant-description"
          :placeholder="rs('VariantDescriptionPlaceholder')"
          :aria-describedby="values.marks['variant.description'] && markId(ids.variantDescription)"
          @update:model-value="(description) => emit('edit', 'variantDescription', description)"
        />
        <ValueMark
          v-if="values.marks['variant.description']"
          :id="markId(ids.variantDescription)"
          :mark="values.marks['variant.description']"
        />
      </Field>

      <div class="grid gap-x-7 gap-y-6 md:grid-cols-3">
        <Field :control-id="ids.prepTime">
          <template #label><ResourceString for="PrepTime" /></template>
          <NumberStepper
            :id="ids.prepTime"
            :model-value="values.prepTime"
            :min="0"
            :unit="rs('Min')"
            v-bind="stepperLabels(rs('PrepTime'), rs('Min'))"
            :aria-describedby="values.marks['variant.prepTime'] && markId(ids.prepTime)"
            @update:model-value="(minutes) => emit('edit', 'prepTime', minutes)"
          />
          <ValueMark
            v-if="values.marks['variant.prepTime']"
            :id="markId(ids.prepTime)"
            :mark="values.marks['variant.prepTime']"
          />
        </Field>

        <Field :control-id="ids.cookTime">
          <template #label><ResourceString for="CookTime" /></template>
          <NumberStepper
            :id="ids.cookTime"
            :model-value="values.cookTime"
            :min="0"
            :unit="rs('Min')"
            v-bind="stepperLabels(rs('CookTime'), rs('Min'))"
            :aria-describedby="values.marks['variant.cookTime'] && markId(ids.cookTime)"
            @update:model-value="(minutes) => emit('edit', 'cookTime', minutes)"
          />
          <ValueMark
            v-if="values.marks['variant.cookTime']"
            :id="markId(ids.cookTime)"
            :mark="values.marks['variant.cookTime']"
          />
        </Field>

        <Field :control-id="ids.servings">
          <template #label><ResourceString for="Servings" /></template>
          <NumberStepper
            :id="ids.servings"
            :model-value="values.servings"
            :min="0"
            v-bind="stepperLabels(rs('Servings'))"
            :aria-describedby="values.marks['variant.servings'] && markId(ids.servings)"
            @update:model-value="(servings) => emit('edit', 'servings', servings)"
          />
          <ValueMark
            v-if="values.marks['variant.servings']"
            :id="markId(ids.servings)"
            :mark="values.marks['variant.servings']"
          />
        </Field>
      </div>

      <StepNav :can-go-back :can-proceed :returns-to-review @back="emit('back')" @review="emit('review')" />
    </form>
  </KccSheet>
</template>
