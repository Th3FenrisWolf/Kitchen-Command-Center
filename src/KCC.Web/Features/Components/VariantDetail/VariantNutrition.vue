<script setup lang="ts">
  import { computed } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import type { Nutrition } from '~/Types/Recipe'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import { buildNutritionRows, hasNutrition } from './variantNutritionRows'

  // Flattened rather than one `Nutrition` object, so the server hydrates each figure as its own prop.
  const props = defineProps<Nutrition>()

  const rs = useResourceStrings()

  const HEADLINE: readonly (keyof Nutrition)[] = ['calories', 'proteinG', 'carbsG', 'fatG']
  const MACROS: readonly (keyof Nutrition)[] = ['proteinG', 'carbsG', 'fatG']

  const provided = computed(() => hasNutrition(props))

  const rows = computed(() =>
    buildNutritionRows(props, {
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

  const headline = computed(() => rows.value.filter((row) => HEADLINE.includes(row.key)))
  const rest = computed(() => rows.value.filter((row) => !HEADLINE.includes(row.key)))

  const bars = computed(() => {
    // A zero-gram macro would draw a kick label over an empty band.
    const macros = rows.value.filter((row) => MACROS.includes(row.key) && row.value > 0)
    const grams = macros.reduce((sum, row) => sum + row.value, 0)
    if (!grams) return []
    return macros.map((row) => ({
      key: row.key,
      label: row.label,
      width: `${Math.round((row.value / grams) * 1000) / 10}%`,
    }))
  })
</script>

<template>
  <KccSheet as="section" icon="fa-duotone fa-wheat" :tear="2">
    <template #label><ResourceString for="Nutrition" /></template>

    <h2 class="sr-only">{{ rs('Nutrition') }}</h2>

    <ResourceString for="PerServing" as="p" class="kcc-kick" />

    <template v-if="provided">
      <div v-if="headline.length" class="kcc-stats mt-6">
        <div v-for="row in headline" :key="row.key">
          <p class="kcc-lbl">{{ row.label }}</p>
          <p class="kcc-v">
            {{ row.value }}<small v-if="row.unit">{{ row.unit }}</small>
          </p>
        </div>
      </div>

      <!-- Each fill is 6px centred in a 24px band, so the run of bars keeps to the rule. -->
      <div v-if="bars.length" class="mt-6">
        <div v-for="bar in bars" :key="bar.key">
          <p class="kcc-kick">{{ bar.label }}</p>
          <div class="flex h-6 items-center">
            <span class="block h-1.5 bg-peach" :style="{ width: bar.width }"></span>
          </div>
        </div>
      </div>

      <dl v-if="rest.length" class="mt-6 grid grid-cols-[1fr_auto] items-baseline gap-x-7">
        <template v-for="row in rest" :key="row.key">
          <dt class="kcc-lbl">{{ row.label }}</dt>
          <dd class="kcc-num text-right">
            {{ row.value }} <span v-if="row.unit" class="kcc-unit">{{ row.unit }}</span>
          </dd>
        </template>
      </dl>
    </template>

    <ResourceString v-else for="NutritionNotProvided" as="p" class="kcc-body mt-6" />
  </KccSheet>
</template>
