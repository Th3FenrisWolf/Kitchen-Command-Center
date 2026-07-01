<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import ComingSoonBadge from '~/Components/ComingSoon/ComingSoonBadge.vue'
</script>

<template>
  <div class="rounded-3xl bg-bone p-6 shadow-primary">
    <div class="mb-3 flex items-baseline justify-between">
      <h2 class="font-casual text-xl tracking-[1px]"><ResourceString for="Nutrition" /></h2>
      <span class="text-xs text-onyx-light"><ResourceString for="PerServing" /></span>
    </div>
    <div class="grid place-items-center gap-2 rounded-2xl bg-bone-dark/60 px-4 py-8 text-center">
      <ComingSoonBadge />
      <p class="text-sm text-onyx-light"><ResourceString for="NutritionComingSoon" /></p>
    </div>
  </div>
</template>
