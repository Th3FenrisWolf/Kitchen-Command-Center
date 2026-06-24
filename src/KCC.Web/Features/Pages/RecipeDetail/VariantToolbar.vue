<script setup lang="ts">
  import type { SortKey, ViewMode } from '~/Pages/RecipeDetail/variantFilters'
  import { ResourceString, useStrings } from '~/Components/ResourceStrings'

  defineProps<{ tags: string[] }>()

  const search = defineModel<string>('search', { required: true })
  const sort = defineModel<SortKey>('sort', { required: true })
  const tag = defineModel<string>('tag', { required: true })
  const view = defineModel<ViewMode>('view', { required: true })

  const t = useStrings()

  const sorts: { key: SortKey | 'rating'; labelKey: string; disabled: boolean }[] = [
    { key: 'newest', labelKey: 'SortNewest', disabled: false },
    { key: 'fastest', labelKey: 'SortFastest', disabled: false },
    { key: 'rating', labelKey: 'SortTopRated', disabled: true },
  ]

  const segBase = 'cursor-pointer rounded-[13px] px-4 py-1.5 text-sm font-bold transition-colors'
  const toggleBase = 'grid h-[34px] w-10 cursor-pointer place-items-center rounded-[13px] text-[15px] transition-colors'
</script>

<template>
  <div class="sticky top-2 z-10 mt-4 flex flex-wrap items-center gap-3 rounded-2xl bg-bone p-3 shadow-light">
    <div class="relative min-w-[200px] flex-1">
      <i class="fa-solid fa-magnifying-glass absolute top-1/2 left-4 -translate-y-1/2 text-sm text-onyx-light"></i>
      <input
        v-model="search"
        type="search"
        :placeholder="t('SearchVariants')"
        class="w-full rounded-2xl border-none bg-bone-dark py-2.5 pr-4 pl-10 text-base text-onyx outline-none"
      />
    </div>

    <div class="flex items-center gap-2">
      <span class="text-sm font-bold text-onyx-light"><ResourceString for="Sort" /></span>
      <div class="flex rounded-2xl bg-bone-dark p-[3px]">
        <button
          v-for="option in sorts"
          :key="option.key"
          type="button"
          :disabled="option.disabled"
          :title="option.disabled ? t('ComingSoon') : undefined"
          :class="[
            segBase,
            sort === option.key ? 'bg-surface-500 text-bone' : 'text-onyx-light',
            option.disabled ? 'cursor-not-allowed opacity-50' : '',
          ]"
          @click="!option.disabled && (sort = option.key as SortKey)"
        >
          <ResourceString :for="option.labelKey" />
        </button>
      </div>
    </div>

    <div class="flex rounded-2xl bg-bone-dark p-[3px]">
      <button
        type="button"
        :title="t('Grid')"
        :class="[toggleBase, view === 'grid' ? 'bg-surface-500 text-bone' : 'text-onyx-light']"
        @click="view = 'grid'"
      >
        <i class="fa-solid fa-table-cells-large"></i>
      </button>
      <button
        type="button"
        :title="t('List')"
        :class="[toggleBase, view === 'list' ? 'bg-surface-500 text-bone' : 'text-onyx-light']"
        @click="view = 'list'"
      >
        <i class="fa-solid fa-list"></i>
      </button>
    </div>
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
      {{ option }}
    </button>
  </div>
</template>
