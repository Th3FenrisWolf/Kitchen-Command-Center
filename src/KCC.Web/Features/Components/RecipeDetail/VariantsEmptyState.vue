<!-- #region VariantsEmptyState Component -->
<script lang="ts">
  /**
   * Shown when a recipe's variants are all filtered out, offering a way back.
   */
  export default {
    name: 'VariantsEmptyState',
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'

  defineEmits<{ clear: [] }>()
</script>

<template>
  <div v-ink="{ kind: 'sheet', hatch: false }" class="sk-sheet sk-sheet--lg mt-6 grid place-items-center gap-3 text-center">
    <span class="sk-wash" style="--c: var(--color-lavender)" aria-hidden="true"></span>
    <i class="sk-ico fa-duotone fa-bowl-food text-4xl"></i>
    <div class="sk-display text-2xl text-ink"><ResourceString for="NoVariantsMatch" /></div>
    <p class="sk-body"><ResourceString for="TryDifferentFilter" /></p>
    <button type="button" v-ink="'button'" class="sk-btn sk-btn--ghost mt-1.5" @click="'clear'">
      <ResourceString for="ClearFilters" />
    </button>
  </div>
</template>
