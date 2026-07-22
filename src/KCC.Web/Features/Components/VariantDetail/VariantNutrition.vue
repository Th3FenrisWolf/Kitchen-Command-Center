<script setup lang="ts">
  import { computed } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import type { Nutrition } from '~/Types/Recipe'
  import { buildNutritionRows, hasNutrition } from './variantNutritionRows'

  const props = defineProps<{
    calories?: number | null
    proteinG?: number | null
    carbsG?: number | null
    fatG?: number | null
    saturatedFatG?: number | null
    fiberG?: number | null
    sugarG?: number | null
    sodiumMg?: number | null
  }>()

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
  <div class="rounded-3xl bg-bone p-6 shadow-primary">
    <div class="mb-3 flex items-baseline justify-between">
      <h2 class="font-casual text-xl tracking-[1px]"><ResourceString for="Nutrition" /></h2>
      <span class="text-xs text-onyx-light"><ResourceString for="PerServing" /></span>
    </div>

    <dl v-if="provided" class="grid grid-cols-2 gap-3">
      <div v-for="row in rows" :key="row.key" class="flex flex-col rounded-2xl bg-bone-dark p-2.5 text-center">
        <dt class="order-2 mt-1 text-xs text-onyx-light">{{ row.label }}</dt>
        <dd class="order-1 font-casual text-2xl leading-none">
          {{ row.value }}<span v-if="row.unit" class="text-base text-onyx-light"> {{ row.unit }}</span>
        </dd>
      </div>
    </dl>

    <div v-else class="grid place-items-center rounded-2xl bg-bone-dark/60 px-4 py-8 text-center">
      <p class="text-sm text-onyx-light"><ResourceString for="NutritionNotProvided" /></p>
    </div>
  </div>
</template>
