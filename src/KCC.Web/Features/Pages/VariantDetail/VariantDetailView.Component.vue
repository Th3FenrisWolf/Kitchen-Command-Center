<script setup lang="ts">
  import { computed } from 'vue'
  import type { Ingredient, Instruction, Breadcrumb, SiblingVariant } from '~/Types/Recipe'
  import type { ImageItem } from '~/Types/ContentTypes'
  import { brandColorFor } from '~/Utilities/BrandColor'
  import { ResourceString, provideResourceStrings } from '~/Components/ResourceStrings'
  import type { StatTileSpec } from '~/Components/Recipe/StatTiles.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import Breadcrumbs from '~/Components/Breadcrumbs/Breadcrumb.vue'
  import ComingSoonBadge from '~/Components/ComingSoon/ComingSoonBadge.vue'
  import DetailHero from '~/Components/Recipe/DetailHero.vue'
  import StatTiles from '~/Components/Recipe/StatTiles.vue'
  import VariantIngredients from '~/Components/VariantDetail/VariantIngredients.vue'
  import VariantNutrition from '~/Components/VariantDetail/VariantNutrition.vue'
  import VariantInstructions from '~/Components/VariantDetail/VariantInstructions.vue'
  import ComingSoonSection from '~/Components/ComingSoon/ComingSoonSection.vue'
  import VariantSiblings from '~/Components/VariantDetail/VariantSiblings.vue'

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

  const rs = provideResourceStrings(props.resourceStrings, 'VariantDetail')

  const coverImage = computed(() => props.images?.[0]?.Asset?.Url)

  const statTiles = computed<StatTileSpec[]>(() => {
    const tiles: StatTileSpec[] = []
    if (props.prepTime) tiles.push({ icon: 'fa-duotone fa-clock', value: props.prepTime, unit: 'min', label: rs('Prep') })
    if (props.cookTime)
      tiles.push({ icon: 'fa-duotone fa-fire-burner', value: props.cookTime, unit: 'min', label: rs('Cook') })
    if (props.servings) tiles.push({ icon: 'fa-duotone fa-utensils', value: props.servings, label: rs('Count') })
    tiles.push({ dotColor: 'green', comingSoon: true, value: rs('ComingSoon'), label: rs('Difficulty') })
    return tiles
  })
</script>

<template>
  <div class="flex items-center justify-between gap-4 pt-4">
    <Breadcrumbs v-if="breadcrumbs?.length" :items="breadcrumbs" />
    <div class="hidden items-center gap-2 lg:flex">
      <ComingSoonBadge />
      <button
        type="button"
        disabled
        :title="rs('ComingSoon')"
        class="flex-none cursor-not-allowed rounded-2xl bg-surface-500 px-4 py-2 text-bone opacity-60"
      >
        <i class="fa-solid fa-play text-sm"></i> <ResourceString for="CookMode" />
      </button>
    </div>
  </div>

  <DetailHero
    :title="variantName"
    :seed="variantName"
    :description="variantDescription"
    :icon="icon"
    :image="coverImage"
    :authorName="createdByName"
  >
    <template #eyebrow>
      <span><ResourceString for="VariantOf" /> {{ recipeName }}</span>
    </template>

    <template v-if="tags.length" #footer>
      <div class="mt-4 flex flex-wrap gap-2">
        <Badge v-for="tag in tags" :key="tag" :class="`bg-${brandColorFor(tag)}`">{{ tag }}</Badge>
      </div>
    </template>
  </DetailHero>

  <StatTiles :tiles="statTiles" />

  <section class="mt-8 grid items-start gap-4 lg:mx-4 lg:grid-cols-4">
    <div class="flex flex-col gap-4 lg:sticky lg:top-4">
      <VariantIngredients :ingredients="ingredients" :base-servings="servings" />
      <VariantNutrition />
    </div>

    <VariantInstructions class="lg:col-span-3" :instructions="instructions" />
  </section>

  <h2 class="mt-8 flex items-center gap-2.5 font-casual text-2xl tracking-[1px]">
    <i class="fa-solid fa-lightbulb text-lg text-yellow"></i> <ResourceString for="CookNotes" />
  </h2>
  <ComingSoonSection text-key="CookNotesComingSoon" icon="fa-solid fa-lightbulb text-4xl opacity-50" />

  <h2 class="mt-8 flex items-center gap-2.5 font-casual text-2xl tracking-[1px]">
    <i class="fa-solid fa-star text-lg text-peach"></i> <ResourceString for="RatingsReviews" />
  </h2>
  <ComingSoonSection text-key="ReviewsComingSoon" icon="fa-solid fa-star text-4xl opacity-50" />
  <VariantSiblings :variants="siblingVariants" />
</template>
