<script setup lang="ts">
  import { useId } from 'vue'
  import Button from '~/Components/Button/Button.vue'
  import InputField from '~/Components/Forms/InputField.vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import type { Ingredient } from '~/Types/Recipe'
  import StepNav from './StepNav.vue'
  import { markId, STEP_PAD } from './stepSheet'
  import ValueMark from './ValueMark.vue'
  import type { WizardValues } from './wizardValues'

  export interface IngredientsStepProps {
    values: WizardValues
    tear?: Tear
    canGoBack?: boolean
    canProceed?: boolean
    returnsToReview?: boolean
  }

  const {
    values,
    tear,
    canGoBack = false,
    canProceed = false,
    returnsToReview = false,
  } = defineProps<IngredientsStepProps>()

  const emit = defineEmits<{
    edit: [index: number, change: Partial<Ingredient>]
    add: []
    remove: [index: number]
    next: []
    back: []
    review: []
  }>()

  const rs = useResourceStrings()

  const uid = useId()
  const nameId = (index: number) => `${uid}-ingredient-${index}`
</script>

<template>
  <KccSheet margin crisp wide :tear :pad="STEP_PAD" icon="fa-duotone fa-list-check">
    <template #label><ResourceString for="Ingredients" /></template>

    <h2 class="sr-only">{{ rs('Ingredients') }}</h2>

    <form class="flex flex-col gap-y-rule-1" @submit.prevent="emit('next')">
      <ul>
        <li
          v-for="(ingredient, index) in values.ingredients"
          :key="index"
          class="grid auto-rows-[minmax(var(--bl),auto)] grid-cols-[var(--control-sm)_minmax(0,1fr)_minmax(0,1fr)_var(--control-sm)] grid-rows-[repeat(2,calc(var(--bl)*2))] items-start gap-x-2 pb-rule-1 sm:grid-cols-[minmax(0,1fr)_var(--control-sm)_5rem_5rem_var(--control-sm)] sm:grid-rows-[calc(var(--bl)*2)] sm:pb-0"
        >
          <InputField
            :id="nameId(index)"
            :model-value="ingredient.name"
            class="col-span-full sm:col-span-1"
            type="text"
            data-testid="wizard-ingredient-name"
            :aria-label="rs('IngredientName')"
            :placeholder="rs('IngredientNamePlaceholder')"
            :aria-describedby="ingredient.mark && markId(nameId(index))"
            @update:model-value="(name: string) => emit('edit', index, { name })"
          />

          <Button
            :variant="ingredient.isEyeballed ? 'ink' : 'ghost'"
            class="kcc-btn--icon"
            :aria-label="rs('Eyeball')"
            :aria-pressed="ingredient.isEyeballed"
            @click="emit('edit', index, { isEyeballed: !ingredient.isEyeballed })"
          >
            <i :class="ingredient.isEyeballed ? 'fa-duotone fa-eye' : 'fa-duotone fa-eye-slash'" aria-hidden="true"></i>
          </Button>

          <ResourceString v-if="ingredient.isEyeballed" for="ToTaste" class="kcc-kick col-span-2" />

          <template v-else>
            <InputField
              :model-value="ingredient.quantity"
              input-class="kcc-num"
              type="number"
              inputmode="decimal"
              step="any"
              :aria-label="rs('Quantity')"
              :placeholder="rs('QuantityPlaceholder')"
              @update:model-value="(quantity: number) => emit('edit', index, { quantity })"
            />

            <InputField
              :model-value="ingredient.unit"
              type="text"
              :aria-label="rs('Unit')"
              :placeholder="rs('UnitPlaceholder')"
              @update:model-value="(unit: string) => emit('edit', index, { unit })"
            />
          </template>

          <Button
            variant="ghost"
            class="kcc-btn--icon"
            :aria-label="rs('RemoveIngredient')"
            :disabled="values.ingredients.length <= 1"
            @click="emit('remove', index)"
          >
            <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
          </Button>

          <ValueMark v-if="ingredient.mark" :id="markId(nameId(index))" class="col-span-full" :mark="ingredient.mark" />
        </li>
      </ul>

      <div class="kcc-row">
        <Button variant="ghost" @click="emit('add')">
          <i class="fa-duotone fa-plus" aria-hidden="true"></i><ResourceString for="AddIngredient" />
        </Button>
      </div>

      <StepNav :can-go-back :can-proceed :returns-to-review @back="emit('back')" @review="emit('review')" />
    </form>
  </KccSheet>
</template>
