<!-- #region RecipeFilters Component Properties -->
<script lang="ts">
  import { computed, useId } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
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
    /** Ties the row's printed box and text to its native checkbox. */
    id: string
    label: string
    count: number
    selected: boolean
    disabled: boolean
  }

  const uid = useId()

  // Render the full, stable option set on every search so the panel keeps its height as filters
  // change (rather than dropping rows). An option with no matches in the current result set is
  // greyed out and disabled — unless it's currently selected, which must stay toggleable so the
  // user can clear it.
  const toRows = (group: string, options: string[], facets: Record<string, number>, selected: string[]): FilterRow[] =>
    [...new Set([...options, ...selected])]
      .sort((a, b) => a.localeCompare(b))
      .map((label, i) => {
        const count = facets[label] ?? 0
        const isSelected = selected.includes(label)
        return { id: `${uid}-${group}-${i}`, label, count, selected: isSelected, disabled: count === 0 && !isSelected }
      })

  const categories = computed(() =>
    toRows('category', props.categoryOptions, props.categoryFacets, props.selectedCategories),
  )
  const diets = computed(() => toRows('diet', props.dietOptions, props.dietFacets, props.selectedDiets))
  const rangeLabel = computed(() => timeRangeLabel(timeMin.value, timeMax.value, t))
  const minUnit = t('Min')
</script>

<template>
  <KccSheet icon="fa-duotone fa-sliders" :tear="5" crisp>
    <template #label><ResourceString for="Filters" /></template>

    <!-- The sheet's label carries the panel's name in print; the heading carries it in the document. -->
    <ResourceString for="Filters" as="h2" class="sr-only" />
    <div class="flex justify-end">
      <Button variant="text" @click="emit('reset')">
        <ResourceString for="Reset" />
      </Button>
    </div>

    <fieldset class="mt-6">
      <ResourceString for="Category" as="legend" class="kcc-kick" />
      <!-- A checklist row is three direct children of the li, so the box and the text are two labels for the
           same checkbox rather than one wrapper around it: both stay clickable, the grid stays the kit's. -->
      <ul class="kcc-check">
        <li
          v-for="row in categories"
          :key="row.label"
          :class="row.disabled ? 'cursor-not-allowed opacity-40' : 'cursor-pointer'"
        >
          <input
            :id="row.id"
            type="checkbox"
            class="sr-only"
            :checked="row.selected"
            :disabled="row.disabled"
            @change="emit('toggleCategory', row.label)"
          />
          <label :for="row.id" class="kcc-box" :class="{ 'kcc-box--on': row.selected }"></label>
          <label :for="row.id">{{ row.label }}</label>
          <span class="kcc-q">{{ row.count }}</span>
        </li>
      </ul>
    </fieldset>

    <fieldset class="mt-6">
      <ResourceString for="Dietary" as="legend" class="kcc-kick" />
      <ul class="kcc-check">
        <li v-for="row in diets" :key="row.label" :class="row.disabled ? 'cursor-not-allowed opacity-40' : 'cursor-pointer'">
          <input
            :id="row.id"
            type="checkbox"
            class="sr-only"
            :checked="row.selected"
            :disabled="row.disabled"
            @change="emit('toggleDiet', row.label)"
          />
          <label :for="row.id" class="kcc-box" :class="{ 'kcc-box--on': row.selected }"></label>
          <label :for="row.id">{{ row.label }}</label>
          <span class="kcc-q">{{ row.count }}</span>
        </li>
      </ul>
    </fieldset>

    <fieldset class="mt-6">
      <ResourceString for="TotalTime" as="legend" class="kcc-kick" />
      <p class="kcc-num">{{ rangeLabel }}</p>
      <!-- The 20px track plus 2px of air on each side is exactly one 24px rule. -->
      <RangeSlider
        v-model:model-min="timeMin"
        v-model:model-max="timeMax"
        :min="0"
        :max="MAX_TIME"
        :step="5"
        class="my-0.5"
      />
      <p class="kcc-kick flex justify-between">
        <span><span class="kcc-num">0</span> {{ minUnit }}</span>
        <span
          ><span class="kcc-num">{{ MAX_TIME }}+</span> {{ minUnit }}</span
        >
      </p>
    </fieldset>
  </KccSheet>
</template>
