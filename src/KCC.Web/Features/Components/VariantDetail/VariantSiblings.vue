<script setup lang="ts">
  import type { SiblingVariant } from '~/Types/Recipe'
  import { ResourceString } from '~/Components/ResourceStrings'
  import { formatRating } from '~/Components/StarRating/starDisplay'
  import Link from '~/Components/Links/Link.Component.vue'
  import AccentTile from '~/Components/Recipe/AccentTile.vue'

  defineProps<{ variants: SiblingVariant[] }>()
</script>

<template>
  <section v-if="variants.length" class="mt-8">
    <h2 class="mb-4 font-casual text-2xl tracking-[1px]"><ResourceString for="OtherVariants" /></h2>
    <div class="grid gap-3 lg:grid-cols-3">
      <Link
        v-for="sibling in variants"
        :key="sibling.slug"
        :href="sibling.slug"
        class="group flex items-center gap-3 rounded-3xl bg-bone p-4 text-onyx shadow-primary transition-shadow hover:shadow-primary-raised"
      >
        <AccentTile :seed="sibling.name" :icon="sibling.icon || 'fa-solid fa-utensils'" class="size-13 flex-none text-2xl" />
        <span class="min-w-0 flex-1">
          <span class="block font-casual text-xl leading-tight tracking-[1px]">{{ sibling.name }}</span>
          <span
            v-if="sibling.rating > 0 || sibling.totalTime > 0"
            class="mt-0.5 flex items-center gap-1.5 text-sm text-onyx-light"
          >
            <span v-if="sibling.rating > 0" class="inline-flex items-center gap-1">
              <i class="fa-solid fa-star text-peach" aria-hidden="true"></i>{{ formatRating(sibling.rating) }}
            </span>
            <span v-if="sibling.rating > 0 && sibling.totalTime > 0" aria-hidden="true">·</span>
            <span v-if="sibling.totalTime > 0">{{ sibling.totalTime }} min</span>
          </span>
        </span>
        <i class="fa-solid fa-arrow-right flex-none text-onyx-light"></i>
      </Link>
    </div>
  </section>
</template>
