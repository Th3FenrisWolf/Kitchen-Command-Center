<script setup lang="ts">
  import { computed, ref } from 'vue'
  import type { Ingredient, Instruction, Breadcrumb, Nutrition, SiblingVariant } from '~/Types/Recipe'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import StatTiles, { type StatTileSpec } from '~/Components/Recipe/StatTiles.vue'
  import { difficultyTile } from '~/Components/VariantDetail/variantDifficulty'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import DetailHero from '~/Components/Recipe/DetailHero.vue'
  import VariantIngredients from '~/Components/VariantDetail/VariantIngredients.vue'
  import VariantNutrition from '~/Components/VariantDetail/VariantNutrition.vue'
  import VariantInstructions from '~/Components/VariantDetail/VariantInstructions.vue'
  import VariantCookedToggle from '~/Components/VariantDetail/VariantCookedToggle.vue'
  import VariantCookNotes from '~/Components/VariantDetail/VariantCookNotes.vue'
  import VariantReviews from '~/Components/VariantDetail/VariantReviews.vue'
  import VariantSiblings from '~/Components/VariantDetail/VariantSiblings.vue'
  import CookMode from './CookMode.vue'

  export interface VariantDetailViewProps extends Nutrition {
    variantName: string
    variantDescription: string
    icon?: string
    coverImage?: string
    prepTime?: number
    cookTime?: number
    servings?: number
    difficulty?: string
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
  }

  const props = defineProps<VariantDetailViewProps>()

  const rs = provideResourceStrings(props.resourceStrings, 'VariantDetail')

  const cookModeOpen = ref(false)
  const hasInstructions = computed(() => props.instructions.length > 0)
  const openCookMode = () => {
    if (hasInstructions.value) cookModeOpen.value = true
  }

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
  <div class="mt-6 space-y-[72px]">
    <!-- The trail and the controls read as one head with the hero, so they sit 24px off it, not 72px. -->
    <div class="space-y-6">
      <div class="flex items-center justify-between gap-4">
        <Breadcrumbs v-if="breadcrumbs?.length" :items="breadcrumbs" />

        <div class="hidden items-center gap-3 lg:flex">
          <Button
            variant="ghost"
            data-test="cook-mode-open-desktop"
            :disabled="!hasInstructions"
            :title="hasInstructions ? rs('CookMode') : rs('ComingSoon')"
            @click="openCookMode"
          >
            <i class="fa-duotone fa-play" aria-hidden="true"></i><ResourceString for="CookMode" />
          </Button>

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
          <div class="kcc-badges mt-6">
            <span v-for="tag in tags" :key="tag" class="kcc-badge">{{ tag }}</span>
          </div>
        </template>
      </DetailHero>
    </div>

    <KccSheet label="At a glance" icon="fa-duotone fa-gauge" :tear="3">
      <StatTiles :tiles="statTiles" />
    </KccSheet>

    <section class="grid gap-x-7 gap-y-9 md:grid-cols-[minmax(0,2fr)_minmax(0,3fr)]">
      <VariantIngredients :ingredients="ingredients" :base-servings="servings" />
      <VariantInstructions :instructions="instructions" />
    </section>

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

    <VariantCookNotes :variant-guid="variantGuid" :is-authenticated="isAuthenticated" />

    <VariantReviews
      :variant-guid="variantGuid"
      :average-rating="averageRating"
      :review-count="reviewCount"
      :is-authenticated="isAuthenticated"
    />

    <VariantSiblings :variants="siblingVariants" />

    <!-- Fixed UI never lives inside a slip: the tilt and the fibre filter would become its containing block. -->
    <CookMode
      :open="cookModeOpen"
      :instructions="instructions"
      :ingredients="ingredients"
      :servings="servings"
      :resource-strings="resourceStrings"
      @close="cookModeOpen = false"
    />
  </div>
</template>
