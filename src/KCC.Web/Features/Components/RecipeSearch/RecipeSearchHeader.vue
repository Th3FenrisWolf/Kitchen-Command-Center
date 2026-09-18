<!-- #region RecipeSearchHeader Component Properties -->
<script lang="ts">
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import Button from '~/Components/Button/Button.vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  /**
   * The library sheet: the query field, the way to a new recipe, and a slot for the results toolbar.
   */
  export default {
    name: 'RecipeSearchHeader',
  }

  export interface RecipeSearchHeaderProps {
    /**
     * Kentico hands out tilde-relative paths; the tilde is stripped before it reaches the anchor.
     */
    createRecipeUrl: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  defineProps<RecipeSearchHeaderProps>()

  // Uncommitted query text: the page only searches once `submit` fires, so this stays separate
  // from the search state itself.
  const draft = defineModel<string>('draft', { required: true })
  const emit = defineEmits<{ submit: []; clear: [] }>()
  const t = useResourceStrings()
  const searchPlaceholder = t('SearchPlaceholder')
  const clearLabel = t('ClearAll')
</script>

<template>
  <KccSheet
    as="section"
    label="Library"
    icon="fa-duotone fa-book-open"
    wash="green"
    :at="{ x: '92%', y: '100%', w: '30%', h: '80%' }"
    :tear="3"
    pad="24px 36px 24px 24px"
    class="my-6"
  >
    <ResourceString for="BrowseTheKitchen" as="p" class="kcc-kick" />

    <div class="flex flex-wrap items-center justify-between gap-x-7 gap-y-3">
      <ResourceString for="SearchRecipes" as="h1" class="kcc-h3" />
      <Button as="a" :href="createRecipeUrl.stripTilde()">
        <i class="fa-duotone fa-plus" aria-hidden="true"></i><ResourceString for="CreateRecipe" />
      </Button>
    </div>

    <form class="mt-6 flex flex-wrap items-center gap-3" @submit.prevent="emit('submit')">
      <div class="kcc-field min-w-0 flex-1 grid-cols-[auto_1fr_auto]">
        <i class="fa-duotone fa-magnifying-glass" aria-hidden="true"></i>
        <input v-model="draft" :placeholder="searchPlaceholder" data-testid="recipe-search-input" />
        <button
          v-if="draft.length"
          type="button"
          class="cursor-pointer text-ink-soft"
          :aria-label="clearLabel"
          @click="emit('clear')"
        >
          <i class="fa-duotone fa-xmark" aria-hidden="true"></i>
        </button>
      </div>
      <Button variant="ghost" type="submit" data-testid="recipe-search-submit">
        <ResourceString for="Search" />
      </Button>
    </form>

    <div v-if="$slots.default" class="mt-6"><slot /></div>
  </KccSheet>
</template>
