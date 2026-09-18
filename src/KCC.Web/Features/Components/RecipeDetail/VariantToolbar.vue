<!-- #region VariantToolbar Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import type { SortKey, ViewMode } from '~/Components/RecipeDetail/variantFilters'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import SegmentedControl, { type SegmentOption } from '~/Components/Recipe/SegmentedControl.vue'

  /**
   * Search, tag, sort, and grid/list controls above a recipe's variants.
   */
  export default {
    name: 'VariantToolbar',
  }

  export interface VariantToolbarProps {
    /**
     * Every tag across the recipe's variants, feeding the tag pills.
     */
    tags: string[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  defineProps<VariantToolbarProps>()

  // Filters apply as they change; there is no submit step.
  const search = defineModel<string>('search', { required: true })
  const sort = defineModel<SortKey>('sort', { required: true })
  const tag = defineModel<string>('tag', { required: true })
  const view = defineModel<ViewMode>('view', { required: true })

  const rs = useResourceStrings()

  const sortOptions = computed<SegmentOption<SortKey>[]>(() => [
    { value: 'newest', label: rs('SortNewest'), testId: 'sort-newest' },
    { value: 'fastest', label: rs('SortFastest'), testId: 'sort-fastest' },
    { value: 'rating', label: rs('SortTopRated'), testId: 'sort-rating' },
  ])

  const viewOptions = computed<SegmentOption<ViewMode>[]>(() => [
    {
      value: 'grid',
      icon: 'fa-duotone fa-table-cells-large',
      ariaLabel: rs('Grid'),
      title: rs('Grid'),
      testId: 'view-grid',
    },
    { value: 'list', icon: 'fa-duotone fa-list', ariaLabel: rs('List'), title: rs('List'), testId: 'view-list' },
  ])
</script>

<template>
  <div class="flex flex-wrap items-center gap-x-7 gap-y-3">
    <div class="kcc-field min-w-50 flex-1">
      <i class="fa-duotone fa-magnifying-glass" aria-hidden="true"></i>
      <input v-model="search" type="search" :placeholder="rs('SearchVariants')" />
    </div>

    <div class="flex items-center gap-3">
      <ResourceString for="Sort" class="kcc-kick" />
      <SegmentedControl v-model="sort" :options="sortOptions" :aria-label="rs('Sort')" />
    </div>

    <!-- Literal group label: there's no `View` resource string (no DB row), so rs('View') would
         leak the raw key. Per-button Grid/List names come from resource strings. -->
    <SegmentedControl v-model="view" :options="viewOptions" variant="icon" aria-label="View" />
  </div>

  <!-- `tagOptions` always opens with the All option, so a recipe whose variants carry no tags has
       nothing to filter by and the row is left off. -->
  <div v-if="tags.length > 1" class="mt-6 flex flex-wrap gap-3">
    <Button
      v-for="option in tags"
      :key="option"
      :variant="tag === option ? 'ink' : 'ghost'"
      :aria-pressed="tag === option"
      @click="tag = option"
    >
      {{ option || rs('All') }}
    </Button>
  </div>
</template>
