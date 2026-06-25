<script setup lang="ts">
  import type { Ingredient, Instruction, Breadcrumb, SiblingVariant } from '~/Types/Recipe'
  import type { ImageItem } from '~/Types/ContentTypes'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumb.vue'
  import VariantHero from './VariantHero.vue'
  import VariantStatTiles from './VariantStatTiles.vue'
  import VariantIngredients from './VariantIngredients.vue'
  import VariantNutrition from './VariantNutrition.vue'
  import VariantInstructions from './VariantInstructions.vue'
  import VariantCookNotes from './VariantCookNotes.vue'
  import VariantReviews from './VariantReviews.vue'
  import VariantSiblings from './VariantSiblings.vue'
  import Badge from '~/Components/Badge/Badge.vue'

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

  useResourceStrings(props.resourceStrings, 'VariantDetail')

  // Deterministic accent palette for tag badges (matches the design's varied chips).
  const tagAccents = ['peach', 'yellow', 'maroon', 'sky', 'green', 'lavender'] as const
  const tagColor = (index: number) => tagAccents[index % tagAccents.length]
</script>

<template>
  <div class="pt-5">
    <Breadcrumbs v-if="breadcrumbs?.length" :items="breadcrumbs" />
  </div>

  <VariantHero
    :variant-name="variantName"
    :recipe-name="recipeName"
    :recipe-slug="recipeSlug"
    :images="images"
    :icon="icon"
    :created-by-name="createdByName"
  />

  <VariantStatTiles :prep-time="prepTime" :cook-time="cookTime" :servings="servings" />

  <section class="mt-6">
    <p class="max-w-[70ch] text-xl leading-normal">{{ variantDescription }}</p>
    <div v-if="tags.length" class="mt-4 flex flex-wrap gap-2">
      <Badge v-for="(tag, i) in tags" :key="tag" :color="tagColor(i)">{{ tag }}</Badge>
    </div>
  </section>

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
