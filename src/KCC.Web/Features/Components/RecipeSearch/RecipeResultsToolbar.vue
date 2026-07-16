<script setup lang="ts">
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import type { RecipeSortKey, RecipeViewMode } from '~/Pages/RecipeSearch/recipeSearchCriteria'

  defineProps<{ heading: string }>()
  const sort = defineModel<RecipeSortKey>('sort', { required: true })
  const view = defineModel<RecipeViewMode>('view', { required: true })
  const t = useResourceStrings()

  const sorts: { key: RecipeSortKey; label: string }[] = [
    { key: 'relevant', label: 'SortRelevant' },
    { key: 'rated', label: 'SortTopRated' },
    { key: 'variants', label: 'SortVariants' },
    { key: 'recent', label: 'SortRecent' },
  ]
  const segBase = 'cursor-pointer rounded-xl border-none px-4 py-2 text-sm font-bold transition-colors'
  const toggleBase = 'grid h-8 w-10 cursor-pointer place-items-center rounded-xl border-none transition-colors'
</script>

<template>
  <div class="mb-2 flex flex-wrap items-center gap-4">
    <h2 class="min-w-40 flex-1 font-casual text-2xl">{{ heading }}</h2>

    <div class="flex items-center gap-2">
      <span class="text-sm font-bold text-onyx-light"><ResourceString for="Sort" /></span>
      <div class="flex rounded-2xl bg-bone-dark p-1">
        <button
          v-for="s in sorts"
          :key="s.key"
          :data-testid="`sort-${s.key}`"
          :class="[segBase, sort === s.key ? 'bg-surface-500 text-bone' : 'bg-transparent text-onyx-light']"
          @click="sort = s.key"
        >
          <ResourceString :for="s.label" />
        </button>
      </div>
    </div>

    <div class="flex rounded-2xl bg-bone-dark p-1">
      <button
        :aria-label="t('Grid')"
        data-testid="view-grid"
        :class="[toggleBase, view === 'grid' ? 'bg-surface-500 text-bone' : 'bg-transparent text-onyx-light']"
        @click="view = 'grid'"
      >
        <i class="fa-solid fa-table-cells-large"></i>
      </button>
      <button
        :aria-label="t('List')"
        data-testid="view-list"
        :class="[toggleBase, view === 'list' ? 'bg-surface-500 text-bone' : 'bg-transparent text-onyx-light']"
        @click="view = 'list'"
      >
        <i class="fa-solid fa-list"></i>
      </button>
    </div>
  </div>
</template>
