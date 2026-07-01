<script setup lang="ts">
  import { computed } from 'vue'
  import type { RecipeVariantSummary } from '~/Types/Recipe'
  import { accentForName } from '~/Pages/RecipeDetail/variantFilters'
  import { ResourceString } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'

  const props = defineProps<{ variant: RecipeVariantSummary }>()
  const accent = computed(() => accentForName(props.variant.name))
</script>

<template>
  <AppLink
    :href="variant.slug"
    class="group grid content-start gap-2 rounded-3xl bg-bone p-4 text-onyx shadow-primary transition-shadow duration-300 hover:shadow-primary-raised focus-within:shadow-primary-raised"
  >
    <img
      v-if="variant.image"
      :src="variant.image"
      :alt="variant.name"
      class="h-40 w-full rounded-2xl object-cover"
    />
    <div
      v-else
      class="grid h-40 w-full place-items-center rounded-2xl text-onyx"
      :class="`bg-${accent}`"
      aria-hidden="true"
    >
      <i :class="variant.icon" class="text-6xl"></i>
    </div>

    <span class="font-casual text-2xl tracking-[1px]">{{ variant.name }}</span>
    <span class="text-xs text-onyx-light">
      <template v-if="variant.authorName"><ResourceString for="By" /> {{ variant.authorName }} · </template>{{ variant.totalTime }} min
    </span>
    <p class="text-sm">{{ variant.description }}</p>

    <div v-if="variant.tags.length" class="flex flex-wrap gap-1">
      <Badge v-for="tag in variant.tags" :key="tag" color="overlay">{{ tag }}</Badge>
    </div>
  </AppLink>
</template>
