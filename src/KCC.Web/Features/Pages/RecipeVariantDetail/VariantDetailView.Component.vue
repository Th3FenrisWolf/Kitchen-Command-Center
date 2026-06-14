<script setup lang="ts">
  import type { Ingredient, Instruction } from '~/Types/Recipe'
  import { ResourceString, useResourceStrings } from '~/Components/ResourceStrings'

  interface SiblingVariant {
    name: string
    slug: string
  }

  const props = defineProps<{
    variantName: string
    variantDescription: string
    imagePaths: string[]
    prepTime?: number
    cookTime?: number
    servings?: number
    tags: string[]
    ingredients: Ingredient[]
    instructions: Instruction[]
    recipeName: string
    recipeSlug: string
    createdByName?: string
    siblingVariants: SiblingVariant[]
    resourceStrings?: Record<string, string>
  }>()

  useResourceStrings(props.resourceStrings, 'VariantDetail')
</script>

<template>
  <SmallHero dark>
    <template #title>{{ variantName }}</template>
    <template #action-button>
      <a :href="recipeSlug" class="rounded-3xl bg-bone px-4 py-2 text-xl text-onyx"> ← {{ recipeName }} </a>
    </template>
  </SmallHero>

  <section class="flex flex-col gap-8">
    <div v-if="imagePaths.length" class="flex gap-4 overflow-x-auto">
      <img
        v-for="(img, i) in imagePaths"
        :key="i"
        :src="img"
        :alt="`${variantName} image ${i + 1}`"
        class="h-64 rounded-3xl object-cover"
      />
    </div>

    <p class="text-lg">{{ variantDescription }}</p>

    <div class="flex flex-wrap gap-4">
      <span v-if="prepTime" class="rounded-full bg-overlay-300 px-3 py-1 text-sm"> Prep: {{ prepTime }} min </span>
      <span v-if="cookTime" class="rounded-full bg-overlay-300 px-3 py-1 text-sm"> Cook: {{ cookTime }} min </span>
      <span v-if="servings" class="rounded-full bg-overlay-300 px-3 py-1 text-sm"> Serves: {{ servings }} </span>
      <span v-if="createdByName" class="rounded-full bg-overlay-300 px-3 py-1 text-sm">
        <ResourceString for="CreatedBy" /> {{ createdByName }}
      </span>
    </div>

    <div v-if="tags.length" class="flex flex-wrap gap-2">
      <span v-for="tag in tags" :key="tag" class="rounded-full bg-surface-500 px-3 py-1 text-sm text-bone">
        {{ tag }}
      </span>
    </div>

    <div>
      <h2 class="mb-4 text-2xl font-bold"><ResourceString for="Ingredients" /></h2>
      <ul class="flex flex-col gap-2">
        <li
          v-for="(ingredient, index) in ingredients"
          :key="index"
          class="flex items-center gap-2 rounded-2xl bg-bone p-3 text-onyx"
        >
          <span v-if="!ingredient.isEyeballed && ingredient.quantity">
            {{ ingredient.quantity }} {{ ingredient.unit }}
          </span>
          <span class="font-bold">{{ ingredient.name }}</span>
          <span v-if="ingredient.isEyeballed" class="italic">to taste</span>
        </li>
      </ul>
    </div>

    <div>
      <h2 class="mb-4 text-2xl font-bold">Instructions</h2>
      <ol class="flex flex-col gap-4">
        <li v-for="(instruction, index) in instructions" :key="index" class="flex gap-4 rounded-2xl bg-bone p-4 text-onyx">
          <span class="font-casual text-3xl text-overlay-300">{{ index + 1 }}</span>
          <span class="pt-1">{{ instruction.text }}</span>
        </li>
      </ol>
    </div>

    <div v-if="siblingVariants.length">
      <h2 class="mb-4 text-2xl font-bold">Other Variants</h2>
      <div class="flex flex-wrap gap-2">
        <a
          v-for="sibling in siblingVariants"
          :key="sibling.slug"
          :href="sibling.slug"
          class="rounded-3xl bg-surface-500 px-4 py-2 text-bone transition-colors duration-300 hover:bg-overlay-200"
        >
          {{ sibling.name }}
        </a>
      </div>
    </div>
  </section>
</template>
