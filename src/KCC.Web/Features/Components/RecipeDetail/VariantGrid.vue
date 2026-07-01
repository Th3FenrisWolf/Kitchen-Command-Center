<script setup lang="ts">
  import type { VariantSummary } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
  import Badge from '~/Components/Badge/Badge.vue'
  import Link from '~/Components/Links/Link.Component.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'

  defineProps<{ variants: VariantSummary[]; addVariantUrl: string }>()
</script>

<template>
  <div class="mt-6 grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
    <Link
      v-for="variant in variants"
      :key="variant.slug"
      :href="variant.slug"
      class="group grid content-start gap-2 rounded-3xl bg-bone p-4 text-onyx shadow-primary transition-shadow focus-within:shadow-primary-raised hover:shadow-primary-raised"
    >
      <AccentTile :seed="variant.name" :icon="variant.icon" :image="variant.image" class="h-40 w-full text-6xl" />

      <span class="font-casual text-2xl">{{ variant.name }}</span>
      <span class="text-xs text-onyx-light">
        <template v-if="variant.authorName">
          <ResourceString for="By" /> {{ variant.authorName }} <i class="fa-solid fa-dot"></i>
        </template>
        {{ variant.totalTime }} <ResourceString for="Min" />
      </span>
      <p class="text-sm">{{ variant.description }}</p>

      <div v-if="variant.tags.length" class="flex flex-wrap gap-1">
        <Badge v-for="tag in variant.tags" :key="tag">{{ tag }}</Badge>
      </div>
    </Link>

    <Link
      :href="addVariantUrl"
      class="group grid min-h-50 place-items-center content-center gap-3 rounded-3xl border-2 border-dashed border-onyx-light text-onyx-light transition-colors hover:border-onyx hover:text-onyx"
    >
      <span
        class="grid h-14 w-14 place-items-center rounded-full bg-onyx-light text-2xl text-bone transition-colors group-hover:bg-onyx"
      >
        <i class="fa-solid fa-plus" />
      </span>
      <span class="font-casual text-2xl"><ResourceString for="AddVariant" /></span>
    </Link>
  </div>
</template>
