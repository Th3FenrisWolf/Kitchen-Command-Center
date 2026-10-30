<script setup lang="ts">
  import { computed } from 'vue'
  import type { SiblingVariant } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
  import RecipeCardView from '~/Components/Recipe/RecipeCard.vue'
  import { siblingToCard } from '~/Components/Recipe/recipeCardModel'
  import { listTearFor } from '~/Utilities/BrandColor'

  export interface VariantSiblingsProps {
    variants: SiblingVariant[]
    flip?: boolean
    tearOffset?: number
  }

  const { variants, flip = false, tearOffset = 0 } = defineProps<VariantSiblingsProps>()

  const cards = computed(() => variants.map((sibling) => ({ key: sibling.slug, card: siblingToCard(sibling) })))
</script>

<template>
  <section v-if="variants.length">
    <div class="kcc-secname"><ResourceString for="OtherVariants" as="h2" class="kcc-section" /></div>

    <div class="grid grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))] gap-x-7 gap-y-rule-1.5">
      <RecipeCardView
        v-for="(entry, index) in cards"
        :key="entry.key"
        :card="entry.card"
        :tear="listTearFor(index + tearOffset)"
        :flip="flip"
      />
    </div>
  </section>
</template>
