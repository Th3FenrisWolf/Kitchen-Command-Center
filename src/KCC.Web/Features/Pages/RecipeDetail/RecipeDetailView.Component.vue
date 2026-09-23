<!-- #region RecipeDetailView Component Properties -->
<script lang="ts">
  import { computed, ref } from 'vue'
  import type { Breadcrumb, VariantSummary } from '~/Types/Recipe'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import { type SortKey, type ViewMode, filterVariants, tagOptions } from '~/Components/RecipeDetail/variantFilters.ts'
  import { averageMinutes, contributorCount, featuredVariant } from '~/Components/RecipeDetail/variantStats'
  import type { StatTileSpec } from '~/Components/Recipe/StatTiles.vue'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import DetailHero from '~/Components/Recipe/DetailHero.vue'
  import StatTiles from '~/Components/Recipe/StatTiles.vue'
  import FeaturedRecipeCard from '~/Components/Recipe/FeaturedRecipeCard.vue'
  import VariantToolbar from '~/Components/RecipeDetail/VariantToolbar.vue'
  import VariantGrid from '~/Components/RecipeDetail/VariantGrid.vue'
  import VariantList from '~/Components/RecipeDetail/VariantList.vue'
  import VariantsEmptyState from '~/Components/RecipeDetail/VariantsEmptyState.vue'
  import { variantToFeatured } from '~/Components/Recipe/recipeCardModel.ts'

  /**
   * A recipe and every variant of it, filtered and sorted client-side.
   */
  export default {
    name: 'RecipeDetailView',
  }

  export interface RecipeDetailViewProps {
    recipeName: string
    recipeDescription: string
    recipeImagePath?: string
    recipeIcon?: string
    recipeCategory?: string
    /**
     * Identifies the recipe to the add-variant page.
     */
    recipeGuid: string
    /**
     * Aggregated across the recipe's variants, since only variants can be reviewed.
     */
    recipeAverageRating?: number
    recipeReviewCount?: number
    recipeTimesCooked?: number
    addVariantUrl: string
    /**
     * Member who created the recipe, as opposed to any variant of it.
     */
    startedByName?: string
    variants: VariantSummary[]
    breadcrumbs?: Breadcrumb[]
    /**
     * Localized text for this page, keyed by unprefixed name and provided to descendants.
     */
    resourceStrings?: Record<string, string>
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<RecipeDetailViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'RecipeDetail')

  const addVariantHref = computed(() => `${props.addVariantUrl.stripTilde()}?recipe=${encodeURIComponent(props.recipeGuid)}`)

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

  const clearFilters = () => {
    search.value = ''
    tag.value = ''
  }
</script>

<template>
  <Breadcrumbs v-if="breadcrumbs?.length" :items="breadcrumbs" class="mt-6" />

  <div class="mt-6 space-y-[72px]">
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
        <span v-if="recipeCategory && startedByName" aria-hidden="true"> · </span>
        <span v-if="startedByName"><ResourceString for="StartedBy" /> {{ startedByName }}</span>
      </template>

      <template #footer>
        <Button as="a" :href="addVariantHref" class="mt-6">
          <i class="fa-duotone fa-plus" aria-hidden="true"></i><ResourceString for="AddVariant" />
        </Button>
      </template>
    </DetailHero>

    <KccSheet label="At a glance" icon="fa-duotone fa-gauge" :tear="3">
      <StatTiles :tiles="statTiles" />
    </KccSheet>

    <section>
      <div class="kcc-secname">
        <ResourceString for="AllVariants" as="h2" />
        <p class="kcc-kick">
          <span class="kcc-num">{{ filtered.length }}</span> {{ rs('Of') }}
          <span class="kcc-num">{{ variants.length }}</span>
        </p>
      </div>

      <div class="space-y-9">
        <FeaturedRecipeCard v-if="featured" :card="variantToFeatured(featured, rs)" />
        <VariantToolbar v-model:search="search" v-model:sort="sort" v-model:tag="tag" v-model:view="view" :tags="tags" />
        <VariantGrid v-if="view === 'grid' && filtered.length" :variants="filtered" :add-variant-url="addVariantHref" />
        <VariantList v-else-if="view === 'list' && filtered.length" :variants="filtered" />
        <VariantsEmptyState v-else-if="!filtered.length" @clear="clearFilters" />
      </div>
    </section>
  </div>
</template>
