<script setup lang="ts">
  import { computed, ref } from 'vue'
  import type { Breadcrumb, VariantSummary } from '~/Types/Recipe'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import { type SortKey, type ViewMode, filterVariants, tagOptions } from '~/Components/RecipeDetail/variantFilters.ts'
  import { averageMinutes, contributorCount, featuredVariant } from '~/Components/RecipeDetail/variantStats'
  import type { StatTileSpec } from '~/Components/Recipe/StatTiles.vue'
  import Link from '~/Components/Links/Link.Component.vue'
  import RecipeBreadcrumb from '~/Components/Breadcrumbs/Breadcrumb.vue'
  import DetailHero from '~/Components/Recipe/DetailHero.vue'
  import StatTiles from '~/Components/Recipe/StatTiles.vue'
  import FeaturedVariant from '../../Components/RecipeDetail/FeaturedVariant.vue'
  import VariantToolbar from '../../Components/RecipeDetail/VariantToolbar.vue'
  import VariantGrid from '../../Components/RecipeDetail/VariantGrid.vue'
  import VariantList from '../../Components/RecipeDetail/VariantList.vue'
  import VariantsEmptyState from '../../Components/RecipeDetail/VariantsEmptyState.vue'

  const props = defineProps<{
    recipeName: string
    recipeDescription: string
    recipeImagePath?: string
    recipeIcon?: string
    recipeCategory?: string
    recipeGuid: string
    recipeAverageRating?: number
    recipeReviewCount?: number
    recipeTimesCooked?: number
    addVariantUrl: string
    startedByName?: string
    variants: VariantSummary[]
    breadcrumbs?: Breadcrumb[]
    resourceStrings?: Record<string, string>
  }>()

  const rs = provideResourceStrings(props.resourceStrings, 'RecipeDetail')

  const addVariantHref = computed(() => `${props.addVariantUrl}?recipe=${encodeURIComponent(props.recipeGuid)}`)

  const search = ref('')
  const tag = ref('')
  const sort = ref<SortKey>('newest')
  const view = ref<ViewMode>('grid')

  const tags = computed(() => tagOptions(props.variants))
  const filtered = computed(() => filterVariants(props.variants, { search: search.value, tag: tag.value, sort: sort.value }))
  const featured = computed(() => featuredVariant(props.variants))
  const fastestMinutes = computed(() =>
    props.variants.length ? Math.min(...props.variants.map((variant) => variant.totalTime)) : null,
  )

  const statTiles = computed<StatTileSpec[]>(() => [
    { icon: 'fa-duotone fa-layer-group', value: props.variants.length, label: rs('Variants') },
    { icon: 'fa-duotone fa-bolt', value: fastestMinutes.value, unit: rs('Min'), label: rs('Fastest') },
    { icon: 'fa-duotone fa-clock', value: averageMinutes(props.variants), unit: rs('Min'), label: rs('AvgTime') },
    { icon: 'fa-duotone fa-users', value: contributorCount(props.variants), label: rs('Contributors') },
  ])

  const resultLabel = computed(() => `${filtered.value.length} ${rs('Of')} ${props.variants.length}`)

  const clearFilters = () => {
    search.value = ''
    tag.value = ''
  }
</script>

<template>
  <div class="mt-4 flex items-center justify-between gap-4">
    <RecipeBreadcrumb v-if="breadcrumbs?.length" :items="breadcrumbs" />

    <Link
      :href="addVariantHref"
      class="inline-flex items-center gap-2 rounded-2xl bg-surface-500 px-3 py-2 text-bone transition-colors hover:bg-surface-400"
    >
      <ResourceString for="AddVariant" />
      <i class="fa-solid fa-plus text-lg" />
    </Link>
  </div>

  <DetailHero
    :title="recipeName"
    :seed="recipeName"
    :description="recipeDescription"
    :icon="recipeIcon"
    :image="recipeImagePath"
    :average-rating="recipeAverageRating"
    :review-count="recipeReviewCount"
    :times-cooked="recipeTimesCooked"
  >
    <template v-if="recipeCategory || startedByName" #eyebrow>
      <span v-if="recipeCategory">{{ recipeCategory }}</span>
      <span v-if="recipeCategory && startedByName"> <i class="fa-solid fa-dot"></i> </span>
      <span v-if="startedByName"><ResourceString for="StartedBy" /> {{ startedByName }}</span>
    </template>
  </DetailHero>

  <StatTiles :tiles="statTiles" />
  <FeaturedVariant v-if="featured" :variant="featured" />

  <section class="mt-8">
    <div class="mb-4 flex items-baseline justify-between gap-4">
      <h2>
        <ResourceString for="AllVariants" />
        <span class="ml-2 font-hazelnut text-lg font-medium text-onyx-light">{{ resultLabel }}</span>
      </h2>
    </div>

    <VariantToolbar v-model:search="search" v-model:sort="sort" v-model:tag="tag" v-model:view="view" :tags="tags" />
    <VariantGrid v-if="view === 'grid' && filtered.length" :variants="filtered" :add-variant-url="addVariantHref" />
    <VariantList v-else-if="view === 'list' && filtered.length" :variants="filtered" />
    <VariantsEmptyState v-else-if="!filtered.length" @clear="clearFilters" />
  </section>
</template>
