<script setup lang="ts">
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import Link from '~/Components/Links/Link.Component.vue'

  defineProps<{ createRecipeUrl?: string }>()
  const draft = defineModel<string>('draft', { required: true })
  const emit = defineEmits<{ submit: []; clear: [] }>()
  const t = useResourceStrings()
  const searchPlaceholder = t('SearchPlaceholder')
</script>

<template>
  <section class="rounded-3xl bg-surface-500 p-4 text-bone">
    <div class="flex flex-wrap items-end justify-between gap-4">
      <div>
        <p class="mb-1 text-sm tracking-wide uppercase opacity-75"><ResourceString for="BrowseTheKitchen" /></p>
        <h1 class="font-casual text-4xl leading-tight"><ResourceString for="SearchRecipes" /></h1>
      </div>
      <Link
        v-if="createRecipeUrl"
        :href="createRecipeUrl"
        class="inline-flex items-center gap-2 rounded-2xl bg-bone px-4 py-2 text-lg font-bold text-onyx"
      >
        <i class="fa-solid fa-plus"></i> <ResourceString for="CreateRecipe" />
      </Link>
    </div>

    <form class="mt-4 flex gap-2" @submit.prevent="emit('submit')">
      <div class="relative min-w-0 flex-1">
        <i class="fa-solid fa-magnifying-glass absolute top-1/2 left-4 -translate-y-1/2 text-onyx-light"></i>
        <input
          v-model="draft"
          :placeholder="searchPlaceholder"
          data-testid="recipe-search-input"
          class="w-full rounded-2xl border-none bg-bone px-11 py-4 font-medium text-onyx outline-none"
        />
        <button
          v-if="draft.length"
          type="button"
          class="absolute top-1/2 right-4 grid size-8 -translate-y-1/2 place-items-center rounded-full bg-bone-dark text-xs text-onyx"
          @click="emit('clear')"
        >
          <i class="fa-solid fa-xmark"></i>
        </button>
      </div>
      <button
        type="submit"
        data-testid="recipe-search-submit"
        class="flex-none rounded-2xl bg-surface-300 px-8 font-bold text-bone transition-colors hover:bg-surface-200"
      >
        <ResourceString for="Search" />
      </button>
    </form>
  </section>
</template>
