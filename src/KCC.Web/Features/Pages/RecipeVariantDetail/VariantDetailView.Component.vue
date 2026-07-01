<script setup lang="ts">
  import { computed } from 'vue'
  import type { Ingredient, Instruction, Breadcrumb, SiblingVariant } from '~/Types/Recipe'
  import type { ImageItem } from '~/Types/ContentTypes'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import type { StatTileSpec } from '~/Components/Detail/types'
  import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumb.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import VariantHero from './VariantHero.vue'
  import StatTiles from '~/Components/Detail/StatTiles.vue'
  import VariantIngredients from './VariantIngredients.vue'
  import VariantNutrition from './VariantNutrition.vue'
  import VariantInstructions from './VariantInstructions.vue'
  import VariantCookNotes from './VariantCookNotes.vue'
  import VariantReviews from './VariantReviews.vue'
  import VariantSiblings from './VariantSiblings.vue'

  const props = defineProps<{
    variantName: string
    variantDescription: string
    icon?: string
    images?: ImageItem[]
    prepTime?: number
    cookTime?: number
    servings?: number
    tags: string[]
    ingredients: Ingredient[]
    instructions: Instruction[]
    recipeName: string
    recipeSlug: string
    createdByName?: string
    breadcrumbs?: Breadcrumb[]
    siblingVariants: SiblingVariant[]
    resourceStrings?: Record<string, string>
  }>()

  const t = useResourceStrings(props.resourceStrings, 'VariantDetail')

  const statTiles = computed<StatTileSpec[]>(() => {
    const tiles: StatTileSpec[] = []
    if (props.prepTime) tiles.push({ icon: 'fa-solid fa-clock', value: props.prepTime, unit: 'min', label: t('Prep') })
    if (props.cookTime) tiles.push({ icon: 'fa-solid fa-fire-burner', value: props.cookTime, unit: 'min', label: t('Cook') })
    if (props.servings) tiles.push({ icon: 'fa-solid fa-utensils', value: props.servings, label: t('Count') })
    tiles.push({ dotColor: 'green', comingSoon: true, value: t('ComingSoon'), label: t('Difficulty') })
    return tiles
  })
</script>

<template>
  <div class="flex items-center justify-between gap-4 pt-5">
    <Breadcrumbs v-if="breadcrumbs?.length" :items="breadcrumbs" />
    <div class="hidden items-center gap-2 lg:flex">
      <button
        type="button"
        disabled
        :title="t('ComingSoon')"
        class="flex-none cursor-not-allowed rounded-2xl bg-surface-500 px-4 py-2.5 text-bone opacity-60"
      >
        <i class="fa-solid fa-play text-sm" aria-hidden="true"></i> <ResourceString for="CookMode" />
      </button>
      <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
    </div>
  </div>

  <VariantHero
    :variant-name="variantName"
    :recipe-name="recipeName"
    :recipe-slug="recipeSlug"
    :variant-description="variantDescription"
    :tags="tags"
    :images="images"
    :icon="icon"
    :created-by-name="createdByName"
  />

  <StatTiles :tiles="statTiles" />

  <section class="mt-8 grid items-start gap-6 lg:grid-cols-[340px_1fr]">
    <div class="flex flex-col gap-4 lg:sticky lg:top-4">
      <VariantIngredients :ingredients="ingredients" :base-servings="servings" />
      <VariantNutrition />
    </div>
    <VariantInstructions :instructions="instructions" />
  </section>

  <VariantCookNotes />

  <VariantReviews />

  <VariantSiblings :variants="siblingVariants" />
</template>
