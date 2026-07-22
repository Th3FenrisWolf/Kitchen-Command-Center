<script setup lang="ts">
  import { computed, ref } from 'vue'
  import type { Ingredient, Instruction, Breadcrumb, SiblingVariant } from '~/Types/Recipe'
  import type { ImageItem } from '~/Types/ContentTypes'
  import { brandColorFor } from '~/Utilities/BrandColor'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import type { StatTileSpec } from '~/Components/Recipe/StatTiles.vue'
  import { difficultyTile } from '~/Components/VariantDetail/variantDifficulty'
  import Badge from '~/Components/Badge/Badge.vue'
  import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumb.vue'
  import DetailHero from '~/Components/Recipe/DetailHero.vue'
  import StatTiles from '~/Components/Recipe/StatTiles.vue'
  import VariantIngredients from '~/Components/VariantDetail/VariantIngredients.vue'
  import VariantNutrition from '~/Components/VariantDetail/VariantNutrition.vue'
  import VariantInstructions from '~/Components/VariantDetail/VariantInstructions.vue'
  import VariantCookedToggle from '~/Components/VariantDetail/VariantCookedToggle.vue'
  import VariantCookNotes from '~/Components/VariantDetail/VariantCookNotes.vue'
  import VariantReviews from '~/Components/VariantDetail/VariantReviews.vue'
  import VariantSiblings from '~/Components/VariantDetail/VariantSiblings.vue'
  import CookMode from './CookMode.vue'

  const props = defineProps<{
    variantName: string
    variantDescription: string
    icon?: string
    images?: ImageItem[]
    prepTime?: number
    cookTime?: number
    servings?: number
    difficulty?: string
    calories?: number | null
    proteinG?: number | null
    carbsG?: number | null
    fatG?: number | null
    saturatedFatG?: number | null
    fiberG?: number | null
    sugarG?: number | null
    sodiumMg?: number | null
    tags: string[]
    ingredients: Ingredient[]
    instructions: Instruction[]
    recipeName: string
    recipeSlug: string
    createdByName?: string
    breadcrumbs?: Breadcrumb[]
    siblingVariants: SiblingVariant[]
    resourceStrings?: Record<string, string>
    variantGuid: string
    averageRating?: number
    reviewCount?: number
    cookedCount?: number
    hasCooked?: boolean
    isAuthenticated?: boolean
  }>()

  const rs = provideResourceStrings(props.resourceStrings, 'VariantDetail')

  const cookModeOpen = ref(false)
  const hasInstructions = computed(() => props.instructions.length > 0)
  const openCookMode = () => {
    if (hasInstructions.value) cookModeOpen.value = true
  }

  const coverImage = computed(() => props.images?.[0]?.Asset?.Url)

  const statTiles = computed<StatTileSpec[]>(() => {
    const tiles: StatTileSpec[] = []
    if (props.prepTime) tiles.push({ icon: 'fa-duotone fa-clock', value: props.prepTime, unit: 'min', label: rs('Prep') })
    if (props.cookTime)
      tiles.push({ icon: 'fa-duotone fa-fire-burner', value: props.cookTime, unit: 'min', label: rs('Cook') })
    if (props.servings) tiles.push({ icon: 'fa-duotone fa-utensils', value: props.servings, label: rs('Count') })
    const difficulty = difficultyTile(props.difficulty, rs)
    if (difficulty) tiles.push(difficulty)
    return tiles
  })
</script>

<template>
  <div class="mt-4 flex items-center justify-between gap-4">
    <Breadcrumbs v-if="breadcrumbs?.length" :items="breadcrumbs" />
    <div class="hidden items-center gap-2 lg:flex">
      <button
        type="button"
        data-test="cook-mode-open-desktop"
        :disabled="!hasInstructions"
        :title="hasInstructions ? rs('CookMode') : rs('ComingSoon')"
        class="inline-flex items-center gap-2 rounded-2xl bg-surface-500 px-3 py-2 text-bone transition-opacity hover:bg-surface-400 disabled:cursor-not-allowed disabled:opacity-60"
        @click="openCookMode"
      >
        <i class="fa-solid fa-play text-sm" aria-hidden="true"></i>
        <ResourceString for="CookMode" />
      </button>
      <VariantCookedToggle
        :variant-guid="variantGuid"
        :cooked-count="cookedCount"
        :has-cooked="hasCooked"
        :is-authenticated="isAuthenticated"
      />
    </div>
  </div>

  <DetailHero
    :title="variantName"
    :seed="variantName"
    :description="variantDescription"
    :icon="icon"
    :image="coverImage"
    :authorName="createdByName"
    :average-rating="averageRating"
    :review-count="reviewCount"
    :times-cooked="cookedCount"
  >
    <template #eyebrow>
      <span><ResourceString for="VariantOf" /> {{ recipeName }}</span>
    </template>

    <template v-if="tags.length" #footer>
      <div class="mt-4 flex flex-wrap gap-2">
        <Badge v-for="tag in tags" :key="tag" :class="`bg-${brandColorFor(tag)} text-onyx`">{{ tag }}</Badge>
      </div>
    </template>
  </DetailHero>

  <StatTiles :tiles="statTiles" />

  <section class="mt-8 grid items-start gap-4 lg:mx-4 lg:grid-cols-4">
    <div class="flex flex-col gap-4 lg:sticky lg:top-4">
      <VariantIngredients :ingredients="ingredients" :base-servings="servings" />
      <VariantNutrition
        :calories="calories"
        :protein-g="proteinG"
        :carbs-g="carbsG"
        :fat-g="fatG"
        :saturated-fat-g="saturatedFatG"
        :fiber-g="fiberG"
        :sugar-g="sugarG"
        :sodium-mg="sodiumMg"
      />
    </div>

    <VariantInstructions class="lg:col-span-3" :instructions="instructions" />
  </section>

  <VariantCookNotes :variant-guid="variantGuid" :is-authenticated="isAuthenticated" />

  <VariantReviews
    :variant-guid="variantGuid"
    :average-rating="averageRating"
    :review-count="reviewCount"
    :is-authenticated="isAuthenticated"
  />
  <VariantSiblings :variants="siblingVariants" />

  <CookMode
    :open="cookModeOpen"
    :instructions="instructions"
    :ingredients="ingredients"
    :servings="servings"
    :resource-strings="resourceStrings"
    @close="cookModeOpen = false"
  />
</template>
