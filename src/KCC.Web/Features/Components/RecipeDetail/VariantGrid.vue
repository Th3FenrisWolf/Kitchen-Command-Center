<script setup lang="ts">
  import type { VariantSummary } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import RecipeCardView from '~/Components/Recipe/RecipeCard.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'
  import { listTearFor } from '~/Utilities/BrandColor'

  export interface VariantGridProps {
    variants: VariantSummary[]
    addVariantUrl: string
    flip?: boolean
  }

  const { flip = false } = defineProps<VariantGridProps>()
  const rs = useResourceStrings()
</script>

<template>
  <div class="grid grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))] gap-x-7 gap-y-9">
    <RecipeCardView
      v-for="(variant, index) in variants"
      :key="variant.slug"
      :card="variantToCard(variant, rs)"
      :tear="listTearFor(index)"
      :flip
    />

    <div
      class="kcc-slip kcc-slip--fill kcc-slip--lift"
      :class="[{ 'kcc-slip--flip': flip }, `kcc-tear-${listTearFor(variants.length)}`]"
    >
      <a :href="addVariantUrl" class="kcc-torn block">
        <div class="kcc-sheet grid content-center justify-items-center gap-6 text-center" style="--pad: 48px">
          <i class="fa-duotone fa-plus text-4xl text-ink-soft" aria-hidden="true"></i>
          <ResourceString for="AddVariant" as="span" class="kcc-h4" />
        </div>
      </a>

      <span class="kcc-blank" aria-hidden="true"><span></span></span>
    </div>
  </div>
</template>
