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
  <KccSheet crisp wide :tear :pad="STEP_PAD" icon="fa-duotone fa-list-check">
    <template #label><ResourceString for="Ingredients" /></template>

    <h2 class="sr-only">{{ rs('Ingredients') }}</h2>

    <form class="flex flex-col gap-6" @submit.prevent="emit('next')">
      <ul>
        <li
          v-for="(ingredient, index) in values.ingredients"
          :key="index"
          class="grid grid-cols-[36px_minmax(0,1fr)_minmax(0,1fr)_36px] items-center gap-x-2 gap-y-3 pb-9 sm:grid-cols-[minmax(0,1fr)_36px_5rem_5rem_36px] sm:pb-3"
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

          <ValueMark v-if="ingredient.mark" :id="markId(nameId(index))" class="col-span-full mb-3" :mark="ingredient.mark" />
        </li>
      </ul>

      <Button variant="ghost" class="self-start" @click="emit('add')">
        <i class="fa-duotone fa-plus" aria-hidden="true"></i><ResourceString for="AddIngredient" />
      </Button>

      <StepNav :can-go-back :can-proceed :returns-to-review @back="emit('back')" @review="emit('review')" />
    </form>
  </KccSheet>
</template>
