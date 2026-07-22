<script setup lang="ts">
  import { computed } from 'vue'
  import type { SortKey, ViewMode } from '~/Components/RecipeDetail/variantFilters'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import SegmentedControl, { type SegmentOption } from '~/Components/Recipe/SegmentedControl.vue'

  defineProps<{ tags: string[] }>()

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
    { value: 'grid', icon: 'fa-solid fa-table-cells-large', ariaLabel: rs('Grid'), title: rs('Grid'), testId: 'view-grid' },
    { value: 'list', icon: 'fa-solid fa-list', ariaLabel: rs('List'), title: rs('List'), testId: 'view-list' },
  ])
</script>

<template>
  <div class="sticky top-2 z-10 mt-4 flex flex-wrap items-center gap-3 rounded-2xl bg-bone p-3 shadow-light">
    <div class="relative min-w-50 flex-1">
      <i class="fa-solid fa-magnifying-glass absolute top-1/2 left-4 -translate-y-1/2 text-sm text-onyx-light"></i>
      <input
        v-model="search"
        type="search"
        :placeholder="rs('SearchVariants')"
        class="w-full rounded-2xl border-none bg-bone-dark py-2.5 pr-4 pl-10 text-base text-onyx outline-none"
      />
    </div>

    <div class="flex items-center gap-2">
      <span class="text-sm font-bold text-onyx-light"><ResourceString for="Sort" /></span>
      <SegmentedControl v-model="sort" :options="sortOptions" :aria-label="rs('Sort')" />
    </div>

    <!-- Literal group label: there's no `View` resource string (no DB row), so rs('View') would
         leak the raw key. Per-button Grid/List names come from resource strings. -->
    <SegmentedControl v-model="view" :options="viewOptions" variant="icon" aria-label="View" />
  </div>

  <div class="mt-4 flex flex-wrap gap-2">
    <button
      v-for="option in tags"
      :key="option"
      type="button"
      class="cursor-pointer rounded-full border-2 px-4 py-1.5 text-sm font-bold transition-colors"
      :class="tag === option ? 'border-onyx bg-onyx text-bone' : 'border-bone-dark text-onyx'"
      @click="tag = option"
    >
      {{ option || rs('All') }}
    </button>
  </div>
</template>
