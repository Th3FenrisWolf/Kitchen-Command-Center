<!-- #region VariantNutrition Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import type { Nutrition } from '~/Types/Recipe'
  import { buildNutritionRows, hasNutrition } from './variantNutritionRows'

  /**
   * Nutrition panel, replaced by an empty state when no figure has been recorded.
   */
  export default {
    name: 'VariantNutrition',
  }

  /**
   * Flattened rather than a single `Nutrition` object so the server can hydrate each figure as its
   * own prop. Individually optional: a missing one is left out of the table.
   */
  export interface VariantNutritionProps {
    calories?: number | null
    proteinG?: number | null
    carbsG?: number | null
    fatG?: number | null
    saturatedFatG?: number | null
    fiberG?: number | null
    sugarG?: number | null
    sodiumMg?: number | null
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<VariantNutritionProps>()

  const rs = useResourceStrings()

  const nutrition = computed<Nutrition>(() => ({
    calories: props.calories,
    proteinG: props.proteinG,
    carbsG: props.carbsG,
    fatG: props.fatG,
    saturatedFatG: props.saturatedFatG,
    fiberG: props.fiberG,
    sugarG: props.sugarG,
    sodiumMg: props.sodiumMg,
  }))

  const provided = computed(() => hasNutrition(nutrition.value))

  const rows = computed(() =>
    buildNutritionRows(nutrition.value, {
      calories: rs('Calories'),
      proteinG: rs('Protein'),
      carbsG: rs('Carbs'),
      fatG: rs('Fat'),
      saturatedFatG: rs('SaturatedFat'),
      fiberG: rs('Fiber'),
      sugarG: rs('Sugar'),
      sodiumMg: rs('Sodium'),
    }),
  )
</script>

<template>
  <div class="rounded-3xl bg-paper-2 p-6">
    <div class="mb-3 flex items-baseline justify-between">
      <h2 class="font-casual text-xl tracking-[1px]"><ResourceString for="Nutrition" /></h2>
      <span class="text-xs text-ink-soft"><ResourceString for="PerServing" /></span>
    </div>

    <dl v-if="provided" class="grid grid-cols-2 gap-3">
      <div v-for="row in rows" :key="row.key" class="flex flex-col rounded-2xl bg-desk-2 p-2.5 text-center">
        <dt class="order-2 mt-1 text-xs text-ink-soft">{{ row.label }}</dt>
        <dd class="order-1 font-casual text-2xl leading-none">
          {{ row.value }}<span v-if="row.unit" class="text-base text-ink-soft"> {{ row.unit }}</span>
        </dd>
      </div>
    </dl>

    <div v-else class="grid place-items-center rounded-2xl bg-desk-2/60 px-4 py-8 text-center">
      <p class="text-sm text-ink-soft"><ResourceString for="NutritionNotProvided" /></p>
    </div>
  </div>
</template>
