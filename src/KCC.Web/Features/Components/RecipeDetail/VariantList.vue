<script setup lang="ts">
  import type { VariantSummary } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
  import Link from '~/Components/Links/Link.Component.vue'
  import Badge from '~/Components/Badge/Badge.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'

  defineProps<{ variants: VariantSummary[] }>()
</script>

<template>
  <div class="mt-6 flex flex-col gap-3">
    <Link
      v-for="variant in variants"
      :key="variant.slug"
      :href="variant.slug"
      :data-variant-name="variant.name"
      class="flex items-center gap-4 rounded-3xl bg-bone p-4 text-onyx shadow-primary transition-shadow hover:shadow-primary-raised"
    >
      <AccentTile :seed="variant.name" :icon="variant.icon" class="size-18 flex-none text-3xl" />
      <span class="min-w-0 flex-1">
        <span class="flex flex-wrap items-baseline gap-x-3 gap-y-2">
          <span class="font-casual text-2xl">{{ variant.name }}</span>
          <span v-if="variant.tags.length" class="inline-flex flex-wrap gap-1.5">
            <Badge v-for="tag in variant.tags" :key="tag">{{ tag }}</Badge>
          </span>
        </span>
        <span class="mt-1 block text-sm text-onyx-light">
          <template v-if="variant.authorName">
            <ResourceString for="By" /> {{ variant.authorName }} <i class="fa-solid fa-dot"></i>
          </template>
          {{ variant.totalTime }} <ResourceString for="Min" />
        </span>
      </span>
      <span class="flex flex-none items-center gap-8 pr-2 text-onyx-light">
        <span class="hidden text-center sm:block">
          <span class="block font-casual text-2xl text-onyx">{{ variant.totalTime }}<ResourceString for="Min" /></span>
          <span class="text-xs"><ResourceString for="Total" /></span>
        </span>
        <i class="fa-solid fa-arrow-right text-onyx"></i>
      </span>
    </Link>
  </div>
</template>
