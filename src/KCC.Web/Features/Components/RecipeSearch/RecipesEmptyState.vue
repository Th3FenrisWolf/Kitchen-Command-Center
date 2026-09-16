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
    v-ink="{ kind: 'sheet', hatch: false }"
    class="sk-sheet sk-sheet--lg grid place-items-center gap-2 text-center"
  >
    <span class="sk-wash" style="--c: var(--color-lavender)" aria-hidden="true"></span>
    <i class="sk-ico fa-duotone fa-bowl-food text-5xl"></i>
    <div class="sk-display text-3xl text-ink"><ResourceString for="NoRecipesMatch" /></div>
    <p class="sk-body max-w-[60ch]"><ResourceString for="NoRecipesHint" /></p>
    <button v-ink="'button'" class="sk-btn sk-btn--ghost mt-2" @click="emit('clear')">
      <ResourceString for="ClearAllFilters" />
    </button>
  </div>
</template>
