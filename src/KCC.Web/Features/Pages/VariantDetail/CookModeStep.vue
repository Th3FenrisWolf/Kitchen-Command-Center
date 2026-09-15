<!-- #region CookModeStep Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
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
    /**
     * Displayed position, one-based.
     */
    stepNumber: number
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

  const timers = computed(() => parseDurations(props.instruction.text))

  const amounts = computed(() =>
    props.ingredients.map((ingredient) =>
      formatIngredientAmount(ingredient, props.baseServings ?? 0, props.currentServings),
    ),
  )
</script>

<template>
  <div class="flex flex-col gap-6">
    <div class="flex items-start gap-4">
      <span class="grid h-12 w-12 flex-none place-items-center rounded-full bg-surface-500 font-casual text-2xl text-bone">
        {{ stepNumber }}
      </span>
      <p class="min-w-0 flex-1 text-2xl leading-relaxed text-onyx">{{ instruction.text }}</p>
    </div>

    <div v-if="timers.length" class="flex flex-wrap gap-2">
      <StepTimer v-for="timer in timers" :key="timer.id" :seconds="timer.seconds" :label="timer.label" />
    </div>

    <div class="rounded-3xl bg-bone p-5 shadow-primary">
      <h3 class="mb-3 font-casual text-lg tracking-[1px] text-onyx"><ResourceString for="Ingredients" /></h3>
      <ul class="flex flex-col">
        <li
          v-for="(ingredient, i) in ingredients"
          :key="i"
          data-test="cook-ingredient"
          class="border-b border-bone-dark py-2 text-base text-onyx last:border-b-0"
        >
          <b v-if="amounts[i]">{{ amounts[i] }} </b>{{ ingredient.name
          }}<span v-if="ingredient.isEyeballed" class="text-onyx-light italic"> — <ResourceString for="ToTaste" /></span>
        </li>
      </ul>
    </div>
  </div>
</template>
