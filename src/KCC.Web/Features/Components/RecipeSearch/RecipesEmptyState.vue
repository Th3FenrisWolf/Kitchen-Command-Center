<!-- #region RecipesEmptyState Component -->
<script lang="ts">
  /**
   * Shown when a search returns nothing, offering a way back.
   */
  export default {
    name: 'RecipesEmptyState',
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'

  const emit = defineEmits<{ clear: [] }>()
</script>

<template>
  <div
    data-testid="recipes-empty"
    class="grid place-items-center gap-2 rounded-3xl border-2 border-dashed border-rule px-4 py-16 text-center text-ink-soft"
  >
    <i class="fa-solid fa-bowl-food text-5xl opacity-50"></i>
    <div class="font-casual text-3xl text-ink"><ResourceString for="NoRecipesMatch" /></div>
    <p class="m-0 max-w-[60ch] text-base"><ResourceString for="NoRecipesHint" /></p>
    <button
      class="mt-2 rounded-full border-2 border-ink bg-transparent px-4 py-2 text-base font-bold text-ink"
      @click="emit('clear')"
    >
      <ResourceString for="ClearAllFilters" />
    </button>
  </div>
</template>
