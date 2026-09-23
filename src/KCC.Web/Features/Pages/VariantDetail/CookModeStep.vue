<!-- #region CookModeStep Component Properties -->
<script lang="ts">
  import { computed, ref, useId } from 'vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import { formatIngredientAmount } from '~/Components/VariantDetail/variantScaling'
  import { parseDurations } from './useStepTimers'
  import StepTimer from './StepTimer.vue'

  /**
   * One cook-mode step: the instruction, timers parsed out of its text, and its ingredients.
   */
  export default {
    name: 'CookModeStep',
  }

  export interface CookModeStepProps {
    instruction: Instruction
    ingredients: Ingredient[]
    /**
     * Servings the stored amounts were written for; amounts scale by `currentServings / baseServings`.
     */
    baseServings?: number
    currentServings: number
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<CookModeStepProps>()

  const rs = useResourceStrings()
  const uid = useId()
  const toTaste = rs('ToTaste')

  const timers = computed(() => parseDurations(props.instruction.text))

  // This component outlives every step change while the overlay is open, so what the cook has already
  // gathered stays ticked as they move through the method, and clears when the overlay closes.
  const checked = ref<Record<number, boolean>>({})

  const rows = computed(() =>
    props.ingredients.map((ingredient, index) => ({
      index,
      id: `${uid}-${index}`,
      name: ingredient.name,
      quantity:
        formatIngredientAmount(ingredient, props.baseServings ?? 0, props.currentServings) ||
        (ingredient.isEyeballed ? toTaste : ''),
    })),
  )

  const toggle = (index: number) => {
    checked.value = { ...checked.value, [index]: !checked.value[index] }
  }
</script>

<template>
  <div class="flex flex-col gap-6">
    <p class="kcc-h3">{{ instruction.text }}</p>

    <div v-if="timers.length" class="flex flex-wrap gap-9">
      <StepTimer v-for="timer in timers" :key="timer.id" :seconds="timer.seconds" :label="timer.label" />
    </div>

    <div>
      <ResourceString :id="uid" for="Ingredients" as="p" class="kcc-kick" />

      <!-- The box and the name are two labels for one checkbox rather than one wrapper around it, so both
           stay clickable; the sr-only input is out of flow, leaving the kit's three grid tracks to the rest. -->
      <ul class="kcc-check mt-6" :aria-labelledby="uid">
        <li
          v-for="row in rows"
          :key="row.id"
          data-test="cook-ingredient"
          class="cursor-pointer"
          :class="{ 'kcc-done': checked[row.index] }"
        >
          <input :id="row.id" type="checkbox" class="sr-only" :checked="checked[row.index]" @change="toggle(row.index)" />
          <label :for="row.id" class="kcc-box" :class="{ 'kcc-box--on': checked[row.index] }"></label>
          <label :for="row.id">{{ row.name }}</label>
          <span class="kcc-q">{{ row.quantity }}</span>
        </li>
      </ul>
    </div>
  </div>
</template>
