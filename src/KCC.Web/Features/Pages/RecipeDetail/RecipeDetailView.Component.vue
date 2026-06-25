<script setup lang="ts">
  import { computed, ref } from 'vue'
  import type { Breadcrumb, RecipeVariantSummary } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import {
    accentForName,
    featuredVariant,
    filterVariants,
    tagOptions,
    type SortKey,
    type ViewMode,
  } from '~/Pages/RecipeDetail/variantFilters'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import RecipeBreadcrumb from '~/Components/Breadcrumbs/Breadcrumb.vue'
  import RecipeHero from './RecipeHero.vue'
  import FeaturedVariant from './FeaturedVariant.vue'
  import VariantToolbar from './VariantToolbar.vue'
  import VariantGrid from './VariantGrid.vue'
  import VariantList from './VariantList.vue'
  import VariantsEmptyState from './VariantsEmptyState.vue'

  const props = defineProps<{
    recipeName: string
    recipeDescription: string
    recipeImagePath?: string
    recipeIcon?: string
    recipeCategory?: string
    recipeGuid: string
    addVariantUrl: string
    startedByName?: string
    variants: RecipeVariantSummary[]
    breadcrumbs?: Breadcrumb[]
    resourceStrings?: Record<string, string>
  }>()

  useResourceStrings(props.resourceStrings, 'RecipeDetail')

  const addVariantHref = computed(
    () => `${props.addVariantUrl}?recipe=${encodeURIComponent(props.recipeGuid)}`,
  )

  const search = ref('')
  const tag = ref('All')
  const sort = ref<SortKey>('newest')
  const view = ref<ViewMode>('grid')

  const tags = computed(() => tagOptions(props.variants))
  const filtered = computed(() =>
    filterVariants(props.variants, { search: search.value, tag: tag.value, sort: sort.value }),
  )
  const featured = computed(() => featuredVariant(props.variants))
  const heroAccent = computed(() => accentForName(props.recipeName))
  const fastestMinutes = computed(() =>
    props.variants.length ? Math.min(...props.variants.map((variant) => variant.totalTime)) : null,
  )
  const resultLabel = computed(() => `${filtered.value.length} of ${props.variants.length}`)

  const clearFilters = () => {
    search.value = ''
    tag.value = 'All'
  }
</script>

<template>
  <div class="flex items-center justify-between gap-4 pt-5">
    <RecipeBreadcrumb v-if="breadcrumbs?.length" :items="breadcrumbs" />
    <AppLink
      :href="addVariantHref"
      class="hidden flex-none rounded-2xl bg-surface-500 px-4 py-2.5 text-bone transition-colors hover:bg-surface-400 lg:inline-flex"
    >
      ＋ <ResourceString for="AddVariant" />
    </AppLink>
  </div>

  <RecipeHero
    :name="recipeName"
    :category="recipeCategory"
    :started-by-name="startedByName"
    :description="recipeDescription"
    :image="recipeImagePath"
    :icon="recipeIcon"
    :accent="heroAccent"
    :variant-count="variants.length"
    :fastest-minutes="fastestMinutes"
    :add-variant-url="addVariantHref"
  />

  <FeaturedVariant v-if="featured" :variant="featured" />

  <section class="mt-8">
    <div class="mb-4 flex items-baseline justify-between gap-4">
      <h2 class="font-casual text-4xl tracking-[1px]">
        <ResourceString for="AllVariants" />
        <span class="font-hazelnut text-lg font-medium text-onyx-light"> — {{ resultLabel }}</span>
      </h2>
    </div>

    <VariantToolbar v-model:search="search" v-model:sort="sort" v-model:tag="tag" v-model:view="view" :tags="tags" />

    <VariantGrid
      v-if="view === 'grid' && filtered.length"
      :variants="filtered"
      :add-variant-url="addVariantHref"
    />
    <VariantList v-else-if="view === 'list' && filtered.length" :variants="filtered" />
    <VariantsEmptyState v-else-if="!filtered.length" @clear="clearFilters" />
  </section>
</template>
