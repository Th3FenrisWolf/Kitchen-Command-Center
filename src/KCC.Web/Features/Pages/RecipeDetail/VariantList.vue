<script setup lang="ts">
  import type { RecipeVariantSummary } from '~/Types/Recipe'
  import { accentForName } from '~/Pages/RecipeDetail/variantFilters'
  import { ResourceString } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'

  defineProps<{ variants: RecipeVariantSummary[] }>()
</script>

<template>
  <div class="mt-6 flex flex-col gap-3">
    <AppLink
      v-for="variant in variants"
      :key="variant.slug"
      :href="variant.slug"
      class="flex items-center gap-4 rounded-3xl bg-bone p-4 text-onyx shadow-primary transition-shadow duration-300 hover:shadow-primary-raised"
    >
      <span
        class="grid h-[72px] w-[72px] flex-none place-items-center rounded-2xl text-3xl text-onyx"
        :class="`bg-${accentForName(variant.name)}`"
        aria-hidden="true"
      >
        <i :class="variant.icon"></i>
      </span>
      <span class="min-w-0 flex-1">
        <span class="flex flex-wrap items-baseline gap-x-3 gap-y-2">
          <span class="font-casual text-2xl leading-tight tracking-[1px]">{{ variant.name }}</span>
          <span v-if="variant.tags.length" class="inline-flex flex-wrap gap-1.5">
            <Badge v-for="tag in variant.tags" :key="tag" color="overlay">{{ tag }}</Badge>
          </span>
        </span>
        <span class="mt-1 block text-sm text-onyx-light">
          <template v-if="variant.authorName"><ResourceString for="By" /> {{ variant.authorName }} · </template>{{ variant.totalTime }} min
        </span>
      </span>
      <span class="flex flex-none items-center gap-8 pr-2 text-onyx-light">
        <span class="hidden text-center sm:block">
          <span class="block font-casual text-2xl text-onyx">{{ variant.totalTime }}m</span>
          <span class="text-xs"><ResourceString for="Total" /></span>
        </span>
        <i class="fa-solid fa-arrow-right text-onyx"></i>
      </span>
    </AppLink>
  </div>
</template>
