<!-- #region RecipeSearchHeader Component -->
<script lang="ts">
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'

  /**
   * Search page masthead wrapping the query field and its submit/clear buttons.
   */
  export default {
    name: 'RecipeSearchHeader',
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  // Uncommitted query text: the page only searches once `submit` fires, so this stays separate
  // from the search state itself.
  const draft = defineModel<string>('draft', { required: true })
  const emit = defineEmits<{ submit: []; clear: [] }>()
  const t = useResourceStrings()
  const searchPlaceholder = t('SearchPlaceholder')
</script>

<template>
  <section class="my-4 rounded-3xl bg-paper p-6 text-ink">
    <p class="mb-1 text-sm tracking-wide uppercase opacity-75"><ResourceString for="BrowseTheKitchen" /></p>
    <h1 class="font-casual text-4xl leading-tight"><ResourceString for="SearchRecipes" /></h1>

    <form class="mt-4 flex gap-4" @submit.prevent="emit('submit')">
      <div class="relative min-w-0 flex-1">
        <i class="fa-solid fa-magnifying-glass absolute top-1/2 left-4 -translate-y-1/2 text-ink-soft"></i>
        <input
          v-model="draft"
          :placeholder="searchPlaceholder"
          data-testid="recipe-search-input"
          class="w-full rounded-2xl border-none bg-paper-2 px-11 py-4 font-medium text-ink outline-none"
        />
        <button
          v-if="draft.length"
          type="button"
          class="absolute top-1/2 right-4 grid size-8 -translate-y-1/2 place-items-center rounded-full bg-desk-2 text-xs text-ink"
          @click="emit('clear')"
        >
          <i class="fa-solid fa-xmark"></i>
        </button>
      </div>
      <button
        type="submit"
        data-testid="recipe-search-submit"
        class="flex-none cursor-pointer rounded-2xl bg-paper-2 px-8 font-bold text-ink transition-colors hover:bg-desk-2"
      >
        <ResourceString for="Search" />
      </button>
    </form>
  </section>
</template>
