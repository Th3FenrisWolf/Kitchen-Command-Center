<!-- #region VariantSiblings Component Properties -->
<script lang="ts">
  import { computed } from 'vue'
  import type { SiblingVariant } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
  import RecipeCardView from '~/Components/Recipe/RecipeCard.vue'
  import { siblingToCard } from '~/Components/Recipe/recipeCardModel'
  import { listTearFor } from '~/Utilities/BrandColor'

  /**
   * Cross-links to the other variants of the same recipe.
   */
  export default {
    name: 'VariantSiblings',
  }

  export interface VariantSiblingsProps {
    /**
     * Excludes the variant being viewed; an empty list renders nothing.
     */
    variants: SiblingVariant[]
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<VariantSiblingsProps>()

  const cards = computed(() => props.variants.map((sibling) => ({ key: sibling.slug, card: siblingToCard(sibling) })))
</script>

<template>
  <section v-if="variants.length">
    <div class="kcc-secname"><ResourceString for="OtherVariants" as="h2" /></div>

    <div class="grid grid-cols-[repeat(auto-fit,minmax(min(300px,100%),1fr))] gap-x-7 gap-y-9">
      <RecipeCardView v-for="(entry, index) in cards" :key="entry.key" :card="entry.card" :tear="listTearFor(index)" />
    </div>
  </section>
</template>
