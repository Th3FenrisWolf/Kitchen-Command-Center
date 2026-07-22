<script setup lang="ts">
  import { computed } from 'vue'
  import type { VariantSummary } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import Link from '~/Components/Links/Link.Component.vue'
  import RecipeCardView from '~/Components/Recipe/RecipeCard.vue'
  import { variantToCard } from '~/Components/Recipe/recipeCardModel'

  const props = defineProps<{ variants: VariantSummary[]; addVariantUrl: string }>()
  const rs = useResourceStrings()
  const cards = computed(() => props.variants.map((variant) => ({ key: variant.slug, card: variantToCard(variant, rs) })))
</script>

<template>
  <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
    <RecipeCardView v-for="entry in cards" :key="entry.key" :card="entry.card" />

    <Link
      :href="addVariantUrl"
      class="group grid min-h-50 place-items-center content-center gap-3 rounded-3xl border-2 border-dashed border-onyx-light text-onyx-light transition-colors hover:border-onyx hover:text-onyx"
    >
      <span
        class="grid h-14 w-14 place-items-center rounded-full bg-onyx-light text-2xl text-bone transition-colors group-hover:bg-onyx"
      >
        <i class="fa-solid fa-plus" />
      </span>
      <span class="font-casual text-2xl"><ResourceString for="AddVariant" /></span>
    </Link>
  </div>
</template>
