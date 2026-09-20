<!-- #region VariantIngredients Component Properties -->
<script lang="ts">
  import { computed, ref, useId } from 'vue'
  import type { Ingredient } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import NumberStepper from '~/Components/Forms/NumberStepper.vue'
  import { formatIngredientAmount } from './variantScaling'

  /**
   * Checklist of ingredients whose amounts rescale with the chosen serving count.
   */
  export default {
    name: 'VariantIngredients',
  }

  export interface VariantIngredientsProps {
    ingredients: Ingredient[]
    /**
     * Servings the stored amounts were written for. Omit or pass 0 to hide the scaler.
     */
    baseServings?: number
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<VariantIngredientsProps>()

  const rs = useResourceStrings()
  const uid = useId()
  const makesLabel = rs('Makes')
  const toTaste = rs('ToTaste')

  const hasScaler = computed(() => (props.baseServings ?? 0) > 0)
  const current = ref<number | undefined>(props.baseServings && props.baseServings > 0 ? props.baseServings : 1)
  const checked = ref<Record<number, boolean>>({})

  // The stepper's field is briefly empty mid-edit; fall back to the written amounts rather than to one serving.
  const servings = computed(() => current.value ?? props.baseServings ?? 1)

  const rows = computed(() =>
    props.ingredients.map((ingredient, index) => ({
      index,
      id: `${uid}-${index}`,
      name: ingredient.name,
      quantity:
        formatIngredientAmount(ingredient, props.baseServings ?? 0, servings.value) ||
        (ingredient.isEyeballed ? toTaste : ''),
    })),
  )

  const toggle = (index: number) => {
    checked.value = { ...checked.value, [index]: !checked.value[index] }
  }
</script>

<template>
  <KccSheet
    as="section"
    icon="fa-duotone fa-basket-shopping"
    wash="teal"
    :at="{ x: '15%', y: '95%', w: '60%', h: '45%' }"
    :tear="1"
  >
    <template #label><ResourceString for="Ingredients" /></template>

    <!-- The sheet's label carries the panel's name in print; the heading carries it in the document. -->
    <h2 class="sr-only">{{ rs('Ingredients') }}</h2>

    <div class="flex flex-wrap items-center justify-between gap-x-7 gap-y-3">
      <p class="kcc-kick">tick what you have</p>
      <div v-if="hasScaler" class="flex items-center gap-3">
        <ResourceString for="Makes" as="span" class="kcc-lbl" />
        <NumberStepper
          v-model="current"
          :min="1"
          :label="makesLabel"
          :decrease-label="rs('Fewer')"
          :increase-label="rs('More')"
        />
      </div>
    </div>

    <!-- The box and the name are two labels for one checkbox rather than one wrapper around it, so both
         stay clickable; the sr-only input is out of flow, leaving the kit's three grid tracks to the rest. -->
    <ul class="kcc-check mt-6">
      <li v-for="row in rows" :key="row.id" class="cursor-pointer" :class="{ 'kcc-done': checked[row.index] }">
        <input :id="row.id" type="checkbox" class="sr-only" :checked="checked[row.index]" @change="toggle(row.index)" />
        <label :for="row.id" class="kcc-box" :class="{ 'kcc-box--on': checked[row.index] }"></label>
        <label :for="row.id">{{ row.name }}</label>
        <span class="kcc-q">{{ row.quantity }}</span>
      </li>
    </ul>
  </KccSheet>
</template>
