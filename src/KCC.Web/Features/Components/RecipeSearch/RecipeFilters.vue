<script setup lang="ts">
  import { computed, useId } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import RangeSlider from '~/Components/Forms/RangeSlider.vue'
  import { MAX_TIME, timeRangeLabel } from '~/Pages/RecipeSearch/recipeSearchCriteria'
  import type { RecipeFacets, RecipeTaxonomy, TaxonomyGroup } from '~/Types/Recipe'

  export interface RecipeFiltersProps {
    facets: RecipeFacets
    options: RecipeTaxonomy
    selected: Record<TaxonomyGroup, string[]>
  }

  const props = defineProps<RecipeFiltersProps>()
  const timeMin = defineModel<number>('timeMin', { required: true })
  const timeMax = defineModel<number>('timeMax', { required: true })
  const emit = defineEmits<{ toggle: [group: TaxonomyGroup, value: string]; reset: [] }>()

  const t = useResourceStrings()
  const uid = useId()

  const GROUPS = [
    { group: 'categories', facet: 'category', legend: 'Category' },
    { group: 'diets', facet: 'diet', legend: 'Diets' },
    { group: 'styles', facet: 'style', legend: 'Styles' },
  ] as const

  const groups = computed(() =>
    GROUPS.map(({ group, facet, legend }) => ({
      group,
      legend,
      rows: props.options[group].map((label, i) => {
        const count = props.facets[facet][label] ?? 0
        const isSelected = props.selected[group].includes(label)
        return { id: `${uid}-${group}-${i}`, label, count, selected: isSelected, disabled: count === 0 && !isSelected }
      }),
    })).filter(({ rows }) => rows.length > 0),
  )
  const rangeLabel = computed(() => timeRangeLabel(timeMin.value, timeMax.value, t))
  const minUnit = t('Min')
</script>

<template>
  <KccSheet icon="fa-duotone fa-sliders" :tear="5" crisp>
    <template #label><ResourceString for="Filters" /></template>

    <ResourceString for="Filters" as="h2" class="sr-only" />
    <div class="flex justify-end">
      <Button variant="text" @click="emit('reset')">
        <ResourceString for="Reset" />
      </Button>
    </div>

    <fieldset v-for="{ group, legend, rows } in groups" :key="group" class="mt-6">
      <ResourceString :for="legend" as="legend" class="kcc-kick" />
      <!-- The box and the name are two labels for one checkbox rather than one wrapper around it, so both
           stay clickable; the sr-only input is out of flow, leaving the kit's three grid tracks to the rest. -->
      <ul class="kcc-check">
        <li v-for="row in rows" :key="row.label" :class="row.disabled ? 'cursor-not-allowed opacity-40' : 'cursor-pointer'">
          <input
            :id="row.id"
            type="checkbox"
            class="sr-only"
            :checked="row.selected"
            :disabled="row.disabled"
            @change="emit('toggle', group, row.label)"
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
