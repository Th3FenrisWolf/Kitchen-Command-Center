<script setup lang="ts">
  import type { VariantSummary } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardView from '~/Components/Recipe/RecipeCard.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'
  import { listTearFor } from '~/Utilities/BrandColor'

  export interface VariantGridProps {
    variants: VariantSummary[]
    addVariantUrl: string
  }

  defineProps<VariantGridProps>()
  const rs = useResourceStrings()
</script>

<template>
  <div class="grid grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))] gap-x-7 gap-y-9">
    <RecipeCardView
      v-for="(variant, index) in variants"
      :key="variant.slug"
      :card="variantToCard(variant, rs)"
      :tear="listTearFor(index)"
    />

    <div
      class="kcc-slip transition-transform focus-within:-translate-y-1 hover:-translate-y-1"
      :class="`kcc-tear-${listTearFor(variants.length)}`"
    >
      <a :href="addVariantUrl" class="kcc-torn block">
        <div class="kcc-sheet grid justify-items-center gap-6 text-center" style="--pad: 48px">
          <i class="fa-duotone fa-plus text-4xl text-ink-soft" aria-hidden="true"></i>
          <ResourceString for="AddVariant" as="span" class="kcc-h4" />
        </div>
      </a>
    </div>
  </div>
</template>
