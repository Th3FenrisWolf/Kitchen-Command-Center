<!-- #region RecipeSearchView Component Properties -->
<script lang="ts">
  import { computed, ref } from 'vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumb.vue'
  import RecipeSearchHeader from '~/Components/RecipeSearch/RecipeSearchHeader.vue'
  import RecipeFilters from '~/Components/RecipeSearch/RecipeFilters.vue'
  import RecipeResultsToolbar from '~/Components/RecipeSearch/RecipeResultsToolbar.vue'
  import AppliedFilterChips from '~/Components/RecipeSearch/AppliedFilterChips.vue'
  import FeaturedRecipeCard from '~/Components/Recipe/FeaturedRecipeCard.vue'
  import RecipeCard from '~/Components/Recipe/RecipeCard.vue'
  import RecipeListRow from '~/Components/RecipeSearch/RecipeListRow.vue'
  import RecipesEmptyState from '~/Components/RecipeSearch/RecipesEmptyState.vue'
  import { useRecipeSearch } from './useRecipeSearch'
  import { useInfiniteScroll } from '~/Components/RecipeSearch/useInfiniteScroll'
  import { MAX_TIME, chipsFor, activeFilterCount, defaultState, type FilterChip } from './recipeSearchCriteria'
  import type { Breadcrumb, RecipeSearchResponse } from '~/Types/Recipe'
  import { hitToCard, hitToFeatured } from '~/Components/Recipe/recipeCardModel'

  /**
   * Recipe search: query, facet filters, and an infinite-scrolling result grid or list.
   */
  export default {
    name: 'RecipeSearchView',
  }

  export interface RecipeSearchViewProps {
    /**
     * Server-rendered first page. Its unfiltered facets also fix the filter panel's option set,
     * which later filtered responses can only narrow.
     */
    initial: RecipeSearchResponse
    createRecipeUrl: string
    breadcrumbs?: Breadcrumb[]
    /**
     * Localized text for this page, keyed by unprefixed name and provided to descendants.
     */
    resourceStrings?: Record<string, string>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { initial, createRecipeUrl, breadcrumbs, resourceStrings } = defineProps<RecipeSearchViewProps>()

  const rs = provideResourceStrings(resourceStrings, 'RecipeSearch')

  const { state, results, facets, categoryOptions, dietOptions, total, spotlight, loading, hasMore, loadMore } =
    useRecipeSearch(initial)

  const draft = ref('')
  const sheetOpen = ref(false)

  const onSubmit = () => {
    state.query = draft.value
  }

  const onClearSearch = () => {
    draft.value = ''
    state.query = ''
  }

  const toggle = (list: string[], value: string) => {
    const i = list.indexOf(value)
    if (i >= 0) {
      list.splice(i, 1)
    } else {
      list.push(value)
    }
  }

  const clearAll = () => {
    const view = state.view
    Object.assign(state, defaultState())
    state.view = view
    draft.value = ''
  }

  const removeChip = (chip: FilterChip) => {
    if (chip.kind === 'query') {
      onClearSearch()
    } else if (chip.kind === 'category' && chip.value) {
      toggle(state.categories, chip.value)
    } else if (chip.kind === 'diet' && chip.value) {
      toggle(state.diets, chip.value)
    } else if (chip.kind === 'time') {
      state.timeMin = 0
      state.timeMax = MAX_TIME
    }
  }

  const chips = computed(() => chipsFor(state, rs))
  const activeCount = computed(() => activeFilterCount(state))
  const heading = computed(() => {
    const q = state.query.trim()
    if (q) {
      return `${total.value} ${rs('ResultsFor')} “${q}”`
    }
    return `${total.value} ${total.value === 1 ? rs('Recipe') : rs('Recipes')}`
  })

  // Don't repeat the spotlight recipe in the grid/list.
  const listed = computed(() =>
    spotlight.value ? results.value.filter((r) => r.slug !== spotlight.value!.slug) : results.value,
  )

  const { sentinel } = useInfiniteScroll(loadMore)
</script>

<template>
  <div class="mt-4 flex items-center justify-between gap-4">
    <Breadcrumbs v-if="breadcrumbs?.length" :items="breadcrumbs" />

    <AppLink
      :href="createRecipeUrl"
      class="inline-flex flex-none items-center gap-2 rounded-2xl bg-surface-500 px-4 py-2 font-bold text-bone transition-colors hover:bg-surface-400"
    >
      <i class="fa-solid fa-plus"></i> <ResourceString for="CreateRecipe" />
    </AppLink>
  </div>

  <RecipeSearchHeader v-model:draft="draft" @submit="onSubmit" @clear="onClearSearch" />

  <button
    class="mb-4 inline-flex items-center gap-2 rounded-full border-2 border-onyx px-4 py-2 text-sm font-bold lg:hidden"
    :class="sheetOpen ? 'bg-onyx text-bone' : 'text-onyx'"
    :aria-expanded="sheetOpen"
    aria-controls="recipe-filters"
    @click="sheetOpen = !sheetOpen"
  >
    <i class="fa-solid fa-sliders"></i> <ResourceString for="Filters" />
    <span
      v-if="activeCount"
      class="grid min-w-5 place-items-center rounded-full px-1.5 text-xs"
      :class="sheetOpen ? 'bg-bone text-onyx' : 'bg-onyx text-bone'"
      >{{ activeCount }}</span
    >
  </button>

  <div class="grid items-start gap-6 lg:grid-cols-[244px_1fr]">
    <aside
      id="recipe-filters"
      :class="['rounded-3xl bg-bone p-6 shadow-primary lg:sticky lg:top-4 lg:block', { hidden: !sheetOpen }]"
    >
      <RecipeFilters
        :category-facets="facets.category"
        :diet-facets="facets.diet"
        :category-options="categoryOptions"
        :diet-options="dietOptions"
        :selected-categories="state.categories"
        :selected-diets="state.diets"
        v-model:time-min="state.timeMin"
        v-model:time-max="state.timeMax"
        @toggle-category="(c) => toggle(state.categories, c)"
        @toggle-diet="(d) => toggle(state.diets, d)"
        @reset="clearAll"
      />
    </aside>

    <section class="min-w-0" :aria-busy="loading ? 'true' : 'false'" aria-live="polite">
      <RecipeResultsToolbar :heading="heading" v-model:sort="state.sort" v-model:view="state.view" />

      <AppliedFilterChips :chips="chips" @remove="removeChip" @clear-all="clearAll" />

      <FeaturedRecipeCard v-if="spotlight" :card="hitToFeatured(spotlight, rs)" />

      <template v-if="listed.length || spotlight">
        <div
          v-if="state.view === 'grid'"
          class="-mb-4 grid grid-cols-1 gap-x-4 *:row-span-7 *:mb-4 sm:grid-cols-2 lg:grid-cols-3"
        >
          <RecipeCard v-for="recipe in listed" :key="recipe.slug" :card="hitToCard(recipe, rs)" />
        </div>

        <div v-else class="flex flex-col gap-4">
          <RecipeListRow v-for="recipe in listed" :key="recipe.slug" :recipe />
        </div>

        <div v-if="hasMore()" :ref="sentinel" class="flex items-center justify-center py-6 text-sm text-onyx-light">
          <span v-if="loading" class="flex items-center gap-2.5">
            <i class="fa-solid fa-circle-notch fa-spin opacity-60"></i> <ResourceString for="LoadingMore" />
          </span>
        </div>
      </template>

      <RecipesEmptyState v-else @clear="clearAll" />
    </section>
  </div>
</template>
