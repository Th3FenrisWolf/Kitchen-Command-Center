<!-- #region RecipeResultsToolbar Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import SegmentedControl, { type SegmentOption } from '~/Components/Recipe/SegmentedControl.vue'
  import type { RecipeSortKey, RecipeViewMode } from '~/Pages/RecipeSearch/recipeSearchCriteria'

  /**
   * Result-count heading paired with the search sort and grid/list controls.
   */
  export default {
    name: 'RecipeResultsToolbar',
  }

  export interface RecipeResultsToolbarProps {
    /**
     * Already localized and counted by the page, e.g. "42 recipes".
     */
    heading: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  defineProps<RecipeResultsToolbarProps>()
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
    { value: 'grid', icon: 'fa-duotone fa-table-cells-large', ariaLabel: t('Grid'), testId: 'view-grid' },
    { value: 'list', icon: 'fa-duotone fa-list', ariaLabel: t('List'), testId: 'view-list' },
  ])
</script>

<template>
  <div class="flex flex-wrap items-center gap-x-7 gap-y-3">
    <h2 class="kcc-h4 min-w-40 flex-1">{{ heading }}</h2>

    <div class="flex items-center gap-3">
      <ResourceString for="Sort" class="kcc-kick" />
      <SegmentedControl v-model="sort" :options="sortOptions" :aria-label="t('Sort')" />
    </div>

    <!-- Literal group label: there's no `View` resource string (no DB row), so t('View') would
         leak the raw key. Per-button Grid/List names come from resource strings. -->
    <SegmentedControl v-model="view" :options="viewOptions" variant="icon" aria-label="View" />
  </div>
</template>
