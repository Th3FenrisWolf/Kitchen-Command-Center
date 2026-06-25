<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Badge from '~/Components/Badge/Badge.vue'

  defineProps<{
    prepTime?: number
    cookTime?: number
    servings?: number
  }>()
</script>

<template>
  <section class="mt-6 grid grid-cols-4 gap-3">
    <div v-if="prepTime" class="rounded-2xl bg-bone p-4 text-center text-onyx shadow-primary">
      <i class="fa-solid fa-clock text-base text-onyx-light"></i>
      <div class="mt-1.5 font-casual text-2xl leading-none lg:text-3xl">{{ prepTime }}<span class="text-base"> min</span></div>
      <div class="text-sm text-onyx-light"><ResourceString for="Prep" /></div>
    </div>

    <div v-if="cookTime" class="rounded-2xl bg-bone p-4 text-center text-onyx shadow-primary">
      <i class="fa-solid fa-fire-burner text-base text-onyx-light"></i>
      <div class="mt-1.5 font-casual text-2xl leading-none lg:text-3xl">{{ cookTime }}<span class="text-base"> min</span></div>
      <div class="text-sm text-onyx-light"><ResourceString for="Cook" /></div>
    </div>

    <div v-if="servings" class="rounded-2xl bg-bone p-4 text-center text-onyx shadow-primary">
      <i class="fa-solid fa-utensils text-base text-onyx-light"></i>
      <div class="mt-1.5 font-casual text-2xl leading-none lg:text-3xl">{{ servings }}</div>
      <div class="text-sm text-onyx-light"><ResourceString for="Count" /></div>
    </div>

    <div class="rounded-2xl bg-bone p-4 text-center text-onyx shadow-primary">
      <span class="inline-block h-[11px] w-[11px] rounded-full bg-green"></span>
      <div class="mt-1.5"><Badge color="muted"><ResourceString for="ComingSoon" /></Badge></div>
      <div class="mt-1 text-sm text-onyx-light"><ResourceString for="Difficulty" /></div>
    </div>
  </section>
</template>
