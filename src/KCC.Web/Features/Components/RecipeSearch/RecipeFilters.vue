<!-- #region RecipeFilters Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import RangeSlider from '~/Components/Forms/RangeSlider.vue'
  import { MAX_TIME, timeRangeLabel } from '~/Pages/RecipeSearch/recipeSearchCriteria'

  /**
   * Filter panel for the recipe search: category and diet toggles plus a total-time range.
   */
  export default {
    name: 'RecipeFilters',
  }

  export interface RecipeFiltersProps {
    /**
     * Result counts for the current search. Values that match nothing are absent, not zero.
     */
    categoryFacets: Record<string, number>
    dietFacets: Record<string, number>
    /**
     * Every value that exists at all, captured from the initial unfiltered search so the panel
     * can grey out dead options instead of dropping them.
     */
    categoryOptions: string[]
    dietOptions: string[]
    selectedCategories: string[]
    selectedDiets: string[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<RecipeFiltersProps>()
  const timeMin = defineModel<number>('timeMin', { required: true })
  const timeMax = defineModel<number>('timeMax', { required: true })
  const emit = defineEmits<{ toggleCategory: [string]; toggleDiet: [string]; reset: [] }>()

  const t = useResourceStrings()

  interface FilterRow {
    label: string
    count: number
    selected: boolean
    disabled: boolean
  }

  // Render the full, stable option set on every search so the panel keeps its height as filters
  // change (rather than dropping rows). An option with no matches in the current result set is
  // greyed out and disabled — unless it's currently selected, which must stay toggleable so the
  // user can clear it.
  const toRows = (options: string[], facets: Record<string, number>, selected: string[]): FilterRow[] =>
    [...new Set([...options, ...selected])]
      .sort((a, b) => a.localeCompare(b))
      .map((label) => {
        const count = facets[label] ?? 0
        const isSelected = selected.includes(label)
        return { label, count, selected: isSelected, disabled: count === 0 && !isSelected }
      })

  const categories = computed(() => toRows(props.categoryOptions, props.categoryFacets, props.selectedCategories))
  const diets = computed(() => toRows(props.dietOptions, props.dietFacets, props.selectedDiets))
  const rangeLabel = computed(() => timeRangeLabel(timeMin.value, timeMax.value, t))
</script>

<template>
  <div v-ink="'sheet'" class="sk-sheet sk-tabbed relative">
    <div class="mb-2 flex items-center justify-end">
      <!-- The tab is the panel heading: sk-tab keeps the marker-tab look without dropping the landmark. -->
      <h2 class="sk-tab"><i class="fa-duotone fa-sliders" aria-hidden="true"></i><ResourceString for="Filters" /></h2>
      <button class="sk-btn sk-btn--text text-sm" @click="emit('reset')">
        <ResourceString for="Reset" />
      </button>
    </div>

    <fieldset class="mt-4 border-t border-rule pt-4">
      <legend class="mb-2 text-base font-bold"><ResourceString for="Category" /></legend>
      <div class="flex flex-col gap-2">
        <label
          v-for="row in categories"
          :key="row.label"
          class="flex items-center gap-2"
          :class="[
            row.selected ? 'font-bold' : 'font-medium',
            row.disabled ? 'cursor-not-allowed opacity-40' : 'cursor-pointer',
          ]"
        >
          <input
            type="checkbox"
            class="peer sr-only"
            :checked="row.selected"
            :disabled="row.disabled"
            @change="emit('toggleCategory', row.label)"
          />
          <span
            class="grid size-4 flex-none place-items-center rounded-md border-2 transition-colors"
            :class="row.selected ? 'border-marker bg-marker' : 'border-ink-soft'"
          >
            <i class="fa-solid fa-check text-xs text-marker-ink" :class="{ 'opacity-0': !row.selected }"></i>
          </span>
          <span class="flex-1">{{ row.label }}</span>
          <span class="text-sm" :class="row.count === 0 ? 'text-ink-soft' : 'text-ink-soft'">{{ row.count }}</span>
        </label>
      </div>
    </fieldset>

    <fieldset class="mt-4 border-t border-rule pt-4">
      <legend class="mb-4 text-base font-bold"><ResourceString for="Dietary" /></legend>
      <div class="flex flex-col gap-2">
        <label
          v-for="row in diets"
          :key="row.label"
          class="flex items-center gap-2"
          :class="[
            row.selected ? 'font-bold' : 'font-medium',
            row.disabled ? 'cursor-not-allowed opacity-40' : 'cursor-pointer',
          ]"
        >
          <input
            type="checkbox"
            class="sr-only"
            :checked="row.selected"
            :disabled="row.disabled"
            @change="emit('toggleDiet', row.label)"
          />
          <span
            class="grid size-4 flex-none place-items-center rounded-md border-2 transition-colors"
            :class="row.selected ? 'border-marker bg-marker' : 'border-ink-soft'"
          >
            <i class="fa-solid fa-check text-xs text-marker-ink" :class="{ 'opacity-0': !row.selected }"></i>
          </span>
          <span class="flex-1">{{ row.label }}</span>
          <span class="text-sm" :class="row.count === 0 ? 'text-ink-soft' : 'text-ink-soft'">{{ row.count }}</span>
        </label>
      </div>
    </fieldset>

    <fieldset class="mt-4 border-t border-rule pt-4">
      <div class="mb-4 flex items-baseline justify-between">
        <legend class="text-base font-bold"><ResourceString for="TotalTime" /></legend>
        <span class="text-sm font-bold text-ink">{{ rangeLabel }}</span>
      </div>
      <RangeSlider v-model:model-min="timeMin" v-model:model-max="timeMax" :min="0" :max="MAX_TIME" :step="5" />
      <div class="mt-2 flex items-baseline justify-between text-sm text-ink-soft">
        <span>0 min</span>
        <span>{{ MAX_TIME }}+ min</span>
      </div>
    </fieldset>
  </div>
</template>
