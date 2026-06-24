<script setup lang="ts">
  import type { RecipeVariantSummary } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import VariantCard from './VariantCard.vue'

  defineProps<{ variants: RecipeVariantSummary[]; addVariantUrl: string }>()
</script>

<template>
  <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
    <VariantCard v-for="variant in variants" :key="variant.slug" :variant="variant" />
    <AppLink
      :href="addVariantUrl"
      class="grid min-h-[200px] content-center place-items-center gap-3 rounded-3xl border-2 border-dashed border-overlay-300 text-onyx-light transition-colors hover:border-onyx hover:text-onyx"
    >
      <span class="grid h-14 w-14 place-items-center rounded-full bg-surface-500 text-2xl text-bone">＋</span>
      <span class="font-casual text-2xl tracking-[1px]"><ResourceString for="AddVariant" /></span>
    </AppLink>
  </div>
</template>
