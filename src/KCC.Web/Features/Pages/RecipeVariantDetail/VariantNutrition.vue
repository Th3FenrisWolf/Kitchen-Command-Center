<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Badge from '~/Components/Badge/Badge.vue'
</script>

<template>
  <div class="rounded-3xl bg-bone p-6 shadow-primary">
    <div class="mb-3 flex items-baseline justify-between">
      <h3 class="font-casual text-xl tracking-[1px]"><ResourceString for="Nutrition" /></h3>
      <span class="text-xs text-onyx-light"><ResourceString for="PerServing" /></span>
    </div>
    <div class="grid place-items-center gap-2 rounded-2xl bg-bone-dark/60 px-4 py-8 text-center">
      <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
      <p class="text-sm text-onyx-light"><ResourceString for="NutritionComingSoon" /></p>
    </div>
  </div>
</template>
