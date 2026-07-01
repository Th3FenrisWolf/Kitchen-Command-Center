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
  <section class="mt-8">
    <h2 class="mb-3 flex flex-wrap items-center gap-2 font-casual text-2xl tracking-[1px]">
      <i class="fa-solid fa-star text-lg text-peach"></i>
      <ResourceString for="TopVariant" />
      <Badge color="muted"><ResourceString for="RankingComingSoon" /></Badge>
    </h2>

    <AppLink
      :href="variant.slug"
      class="group relative flex flex-col items-start gap-6 rounded-3xl bg-bone p-4 text-onyx shadow-primary transition-shadow duration-300 hover:shadow-primary-raised sm:flex-row sm:items-center"
    >
      <img
        v-if="variant.image"
        :src="variant.image"
        :alt="variant.name"
        class="h-[150px] w-full rounded-2xl object-cover sm:w-[150px]"
      />
      <div
        v-else
        class="grid h-[150px] w-full place-items-center rounded-2xl text-onyx sm:w-[150px]"
        :class="`bg-${accent}`"
        aria-hidden="true"
      >
        <i :class="variant.icon" class="text-6xl"></i>
      </div>

      <div class="min-w-0 flex-1">
        <div v-if="variant.tags.length" class="mb-2 flex flex-wrap gap-1.5">
          <Badge v-for="tag in variant.tags" :key="tag" color="overlay">{{ tag }}</Badge>
        </div>
        <div class="font-casual text-4xl leading-tight tracking-[1px]">{{ variant.name }}</div>
        <p class="mt-1.5 mb-2.5 max-w-[56ch] text-lg">{{ variant.description }}</p>
        <div class="flex flex-wrap items-center gap-x-6 gap-y-1 text-base text-onyx-light">
          <span><i class="fa-solid fa-clock"></i> {{ variant.totalTime }} min</span>
          <span v-if="variant.authorName"><ResourceString for="By" /> {{ variant.authorName }}</span>
        </div>
      </div>

      <span class="absolute right-4 bottom-4 grid h-10 w-10 place-items-center rounded-full bg-surface-500 text-bone">
        <i class="fa-solid fa-arrow-right"></i>
      </span>
    </AppLink>
  </section>
</template>
