<script setup lang="ts">
  import { computed } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import SegmentedControl, { type SegmentOption } from '~/Components/Recipe/SegmentedControl.vue'
  import type { RecipeSortKey, RecipeViewMode } from '~/Pages/RecipeSearch/recipeSearchCriteria'

  defineProps<{ heading: string }>()
  const sort = defineModel<RecipeSortKey>('sort', { required: true })
  const view = defineModel<RecipeViewMode>('view', { required: true })
  const t = useResourceStrings()

  const sortOptions = computed<SegmentOption<RecipeSortKey>[]>(() => [
    { value: 'relevant', label: t('SortRelevant'), testId: 'sort-relevant' },
    { value: 'rated', label: t('SortTopRated'), testId: 'sort-rated' },
    { value: 'variants', label: t('SortVariants'), testId: 'sort-variants' },
    { value: 'recent', label: t('SortRecent'), testId: 'sort-recent' },
  ])

  const viewOptions = computed<SegmentOption<RecipeViewMode>[]>(() => [
    { value: 'grid', icon: 'fa-solid fa-table-cells-large', ariaLabel: t('Grid'), testId: 'view-grid' },
    { value: 'list', icon: 'fa-solid fa-list', ariaLabel: t('List'), testId: 'view-list' },
  ])
</script>

<template>
  <div class="mb-2 flex flex-wrap items-center gap-4">
    <h2 class="min-w-40 flex-1 font-casual text-2xl">{{ heading }}</h2>

    <div class="flex items-center gap-2">
      <span class="text-sm font-bold text-onyx-light"><ResourceString for="Sort" /></span>
      <SegmentedControl v-model="sort" :options="sortOptions" :aria-label="t('Sort')" />
    </div>

    <!-- Literal group label: there's no `View` resource string (no DB row), so t('View') would
         leak the raw key. Per-button Grid/List names come from resource strings. -->
    <SegmentedControl v-model="view" :options="viewOptions" variant="icon" aria-label="View" />
  </div>
</template>
