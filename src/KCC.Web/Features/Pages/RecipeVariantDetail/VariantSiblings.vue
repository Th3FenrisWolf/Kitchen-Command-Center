<script setup lang="ts">
  import type { SiblingVariant } from '~/Types/Recipe'
  import { accentForName } from '~/Pages/RecipeDetail/variantFilters'
  import { ResourceString } from '~/Components/ResourceStrings'
  import AppLink from '~/Components/Links/AppLink.Component.vue'

  defineProps<{ variants: SiblingVariant[] }>()
</script>

<template>
  <section v-if="variants.length" class="mt-8">
    <h2 class="mb-4 font-casual text-2xl tracking-[1px]"><ResourceString for="OtherVariants" /></h2>
    <div class="grid gap-3 lg:grid-cols-3">
      <AppLink
        v-for="sibling in variants"
        :key="sibling.slug"
        :href="sibling.slug"
        class="group flex items-center gap-3 rounded-3xl bg-bone p-4 text-onyx shadow-primary transition-shadow duration-300 hover:shadow-primary-raised"
      >
        <span
          class="grid h-[52px] w-[52px] flex-none place-items-center rounded-2xl text-2xl text-onyx"
          :class="`bg-${accentForName(sibling.name)}`"
          aria-hidden="true"
        >
          <i :class="sibling.icon || 'fa-solid fa-utensils'"></i>
        </span>
        <span class="min-w-0 flex-1">
          <span class="block font-casual text-xl leading-tight tracking-[1px]">{{ sibling.name }}</span>
        </span>
        <i class="fa-solid fa-arrow-right flex-none text-onyx-light"></i>
      </AppLink>
    </div>
  </section>
</template>
