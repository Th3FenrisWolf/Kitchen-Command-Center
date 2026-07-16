<script setup lang="ts">
  import { computed } from 'vue'
  import { ResourceString } from '~/Components/ResourceStrings'
  import RangeSlider from '~/Components/Forms/RangeSlider.vue'
  import { MAX_TIME, timeRangeLabel } from '~/Pages/RecipeSearch/recipeSearchCriteria'

  const props = defineProps<{
    categoryFacets: Record<string, number>
    dietFacets: Record<string, number>
    selectedCategories: string[]
    selectedDiets: string[]
  }>()
  const timeMin = defineModel<number>('timeMin', { required: true })
  const timeMax = defineModel<number>('timeMax', { required: true })
  const emit = defineEmits<{ toggleCategory: [string]; toggleDiet: [string]; reset: [] }>()

  const categories = computed(() => Object.entries(props.categoryFacets).sort(([a], [b]) => a.localeCompare(b)))
  const diets = computed(() => Object.entries(props.dietFacets).sort(([a], [b]) => a.localeCompare(b)))
  const rangeLabel = computed(() => timeRangeLabel(timeMin.value, timeMax.value))
</script>

<template>
  <div>
    <div class="mb-2 flex items-center justify-between">
      <h2 class="font-casual text-2xl"><ResourceString for="Filters" /></h2>
      <button class="text-sm font-bold text-onyx-light underline" @click="emit('reset')">
        <ResourceString for="Reset" />
      </button>
    </div>

    <fieldset class="mt-4 border-t border-bone-dark pt-4">
      <legend class="mb-2 text-base font-bold"><ResourceString for="Category" /></legend>
      <div class="flex flex-col gap-2">
        <label
          v-for="[label, count] in categories"
          :key="label"
          class="flex cursor-pointer items-center gap-2"
          :class="selectedCategories.includes(label) ? 'font-bold' : 'font-medium'"
        >
          <input
            type="checkbox"
            class="peer sr-only"
            :checked="selectedCategories.includes(label)"
            @change="emit('toggleCategory', label)"
          />
          <span
            class="grid size-4 flex-none place-items-center rounded-md border-2 transition-colors"
            :class="selectedCategories.includes(label) ? 'border-onyx bg-onyx' : 'border-onyx-light'"
          >
            <i class="fa-solid fa-check text-xs text-bone" :class="{ 'opacity-0': !selectedCategories.includes(label) }"></i>
          </span>
          <span class="flex-1">{{ label }}</span>
          <span class="text-sm" :class="count === 0 ? 'text-overlay-300' : 'text-onyx-light'">{{ count }}</span>
        </label>
      </div>
    </fieldset>

    <fieldset class="mt-4 border-t border-bone-dark pt-4">
      <legend class="mb-4 text-base font-bold"><ResourceString for="Dietary" /></legend>
      <div class="flex flex-col gap-2">
        <label
          v-for="[label, count] in diets"
          :key="label"
          class="flex cursor-pointer items-center gap-2"
          :class="selectedDiets.includes(label) ? 'font-bold' : 'font-medium'"
        >
          <input
            type="checkbox"
            class="sr-only"
            :checked="selectedDiets.includes(label)"
            @change="emit('toggleDiet', label)"
          />
          <span
            class="grid size-4 flex-none place-items-center rounded-md border-2 transition-colors"
            :class="selectedDiets.includes(label) ? 'border-onyx bg-onyx' : 'border-onyx-light'"
          >
            <i class="fa-solid fa-check text-xs text-bone" :class="{ 'opacity-0': !selectedDiets.includes(label) }"></i>
          </span>
          <span class="flex-1">{{ label }}</span>
          <span class="text-sm" :class="count === 0 ? 'text-overlay-300' : 'text-onyx-light'">{{ count }}</span>
        </label>
      </div>
    </fieldset>

    <fieldset class="mt-4 border-t border-bone-dark pt-4">
      <div class="mb-4 flex items-baseline justify-between">
        <legend class="text-base font-bold"><ResourceString for="TotalTime" /></legend>
        <span class="text-sm font-bold text-onyx">{{ rangeLabel }}</span>
      </div>
      <RangeSlider v-model:model-min="timeMin" v-model:model-max="timeMax" :min="0" :max="MAX_TIME" :step="5" />
      <div class="mt-2 flex items-baseline justify-between text-sm text-onyx-light">
        <span>0 min</span>
        <span>{{ MAX_TIME }}+ min</span>
      </div>
    </fieldset>
  </div>
</template>
