<script setup lang="ts">
  import { computed, ref } from 'vue'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import ComingSoonBadge from '~/Components/ComingSoon/ComingSoonBadge.vue'
  import RecipeSearchHeader from '~/Components/RecipeSearch/RecipeSearchHeader.vue'
  import RecipeFilters from '~/Components/RecipeSearch/RecipeFilters.vue'
  import RecipeResultsToolbar from '~/Components/RecipeSearch/RecipeResultsToolbar.vue'
  import AppliedFilterChips from '~/Components/RecipeSearch/AppliedFilterChips.vue'
  import RecipeSpotlight from '~/Components/RecipeSearch/RecipeSpotlight.vue'
  import RecipeCard from '~/Components/RecipeSearch/RecipeCard.vue'
  import RecipeListRow from '~/Components/RecipeSearch/RecipeListRow.vue'
  import RecipesEmptyState from '~/Components/RecipeSearch/RecipesEmptyState.vue'
  import { useRecipeSearch } from './useRecipeSearch'
  import { useInfiniteScroll } from '~/Components/RecipeSearch/useInfiniteScroll'
  import { MAX_TIME, chipsFor, activeFilterCount, defaultState, type FilterChip } from './recipeSearchCriteria'
  import type { RecipeSearchResponse } from '~/Types/Recipe'

  const props = defineProps<{
    initial: RecipeSearchResponse
    createRecipeUrl?: string
    resourceStrings?: Record<string, string>
  }>()

  const rs = provideResourceStrings(props.resourceStrings, 'RecipeSearch')

  const { state, results, facets, total, spotlight, loading, hasMore, loadMore } = useRecipeSearch(props.initial)

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

  const chips = computed(() => chipsFor(state))
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

  // `loading` is surfaced for future use (e.g. a busy affordance); referenced to satisfy the linter.
  void loading

  const { sentinel } = useInfiniteScroll(loadMore)
</script>

<template>
  <div class="my-8">
    <RecipeSearchHeader
      v-model:draft="draft"
      :create-recipe-url="createRecipeUrl"
      @submit="onSubmit"
      @clear="onClearSearch"
    />

    <p class="mt-2 flex items-center gap-2 text-xs text-onyx-light">
      <ComingSoonBadge /> <ResourceString for="IngredientSearchComingSoon" />
    </p>

    <button
      class="mt-4 inline-flex items-center gap-2 rounded-full border-2 border-onyx px-4 py-2 text-sm font-bold lg:hidden"
      :class="sheetOpen ? 'bg-onyx text-bone' : 'text-onyx'"
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

    <section v-if="sheetOpen" class="mt-3 rounded-3xl bg-bone p-4 shadow-primary lg:hidden">
      <RecipeFilters
        :category-facets="facets.category"
        :diet-facets="facets.diet"
        :selected-categories="state.categories"
        :selected-diets="state.diets"
        v-model:time-min="state.timeMin"
        v-model:time-max="state.timeMax"
        @toggle-category="(c) => toggle(state.categories, c)"
        @toggle-diet="(d) => toggle(state.diets, d)"
        @reset="clearAll"
      />
    </section>

    <div class="mt-6 grid items-start gap-6 lg:grid-cols-[244px_1fr]">
      <aside class="sticky top-3 hidden rounded-3xl bg-bone p-6 shadow-primary lg:block">
        <RecipeFilters
          :category-facets="facets.category"
          :diet-facets="facets.diet"
          :selected-categories="state.categories"
          :selected-diets="state.diets"
          v-model:time-min="state.timeMin"
          v-model:time-max="state.timeMax"
          @toggle-category="(c) => toggle(state.categories, c)"
          @toggle-diet="(d) => toggle(state.diets, d)"
          @reset="clearAll"
        />
      </aside>

      <section class="min-w-0">
        <RecipeResultsToolbar :heading="heading" v-model:sort="state.sort" v-model:view="state.view" />

        <AppliedFilterChips :chips="chips" @remove="removeChip" @clear-all="clearAll" />

        <RecipeSpotlight v-if="spotlight" :recipe="spotlight" />

        <template v-if="listed.length || spotlight">
          <div v-if="state.view === 'grid'" class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <RecipeCard v-for="r in listed" :key="r.slug" :recipe="r" />
          </div>

          <div v-else class="flex flex-col gap-3">
            <RecipeListRow v-for="r in listed" :key="r.slug" :recipe="r" />
          </div>

          <div v-if="hasMore()" ref="sentinel" class="flex items-center justify-center gap-2.5 py-6 text-sm text-onyx-light">
            <i class="fa-solid fa-circle-notch fa-spin opacity-60"></i> <ResourceString for="LoadingMore" />
          </div>
        </template>

        <RecipesEmptyState v-else @clear="clearAll" />
      </section>
    </div>
  </div>
</template>
