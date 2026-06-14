<script setup lang="ts">
  import { cx } from '~/Utilities/CX'
  import type { RecipeVariantSummary } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'
  import SmallHero from '~/Widgets/Hero/SmallHero.Component.vue'
  import AppLink from '~/Components/Links/AppLink.Component.vue'

  const props = defineProps<{
    recipeName: string
    recipeDescription: string
    recipeImagePath?: string
    recipeCategory?: string
    recipeGuid: string
    addVariantUrl: string
    startedByName?: string
    variants: RecipeVariantSummary[]
    resourceStrings?: Record<string, string>
  }>()

  useResourceStrings(props.resourceStrings, 'RecipeDetail')
</script>

<template>
  <SmallHero dark>
    <template #title>{{ recipeName }}</template>
    <template #action-button>
      <AppLink
        :href="`${props.addVariantUrl}?recipe=${encodeURIComponent(props.recipeGuid)}`"
        class="rounded-3xl bg-bone px-4 py-2 text-xl text-onyx"
      >
        Add Variant
      </AppLink>
    </template>
  </SmallHero>

  <section class="flex flex-col gap-8">
    <div class="flex flex-col gap-2">
      <img v-if="recipeImagePath" :src="recipeImagePath" :alt="recipeName" class="h-64 w-full rounded-3xl object-cover" />
      <p class="text-lg">{{ recipeDescription }}</p>
      <p v-if="startedByName" class="text-sm text-onyx-light">
        <ResourceString for="StartedBy" /> {{ startedByName }}
      </p>
      <span v-if="recipeCategory" class="w-fit rounded-full bg-overlay-300 px-3 py-1 text-sm">
        {{ recipeCategory }}
      </span>
    </div>

    <div>
      <h2 class="mb-4 text-2xl font-bold">Variants</h2>
      <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-3">
        <AppLink
          v-for="variant in variants"
          :key="variant.slug"
          :href="variant.slug"
          :class="
            cx(
              'group/card grid gap-2 rounded-3xl bg-bone p-4 text-onyx shadow-primary transition-all duration-300',
              'focus-within:shadow-primary-raised hover:shadow-primary-raised',
            )
          "
        >
          <img v-if="variant.image" :src="variant.image" :alt="variant.name" class="h-40 w-full rounded-2xl object-cover" />
          <div v-else class="flex h-40 w-full items-center justify-center rounded-2xl bg-bone-dark" aria-hidden="true">
            <i :class="variant.icon" class="text-6xl"></i>
          </div>
          <span class="font-casual text-2xl">{{ variant.name }}</span>
          <span v-if="variant.authorName" class="text-xs text-onyx-light">
            <ResourceString for="By" /> {{ variant.authorName }}
          </span>
          <span class="text-sm">{{ variant.description }}</span>
          <div v-if="variant.tags.length" class="flex flex-wrap gap-1">
            <span v-for="tag in variant.tags" :key="tag" class="rounded-full bg-overlay-300 px-2 py-0.5 text-xs">
              {{ tag }}
            </span>
          </div>
        </AppLink>
      </div>
    </div>
  </section>
</template>
