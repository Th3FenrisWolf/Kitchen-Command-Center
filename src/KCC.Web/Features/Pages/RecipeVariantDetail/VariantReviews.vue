<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Badge from '~/Components/Badge/Badge.vue'
</script>

<template>
  <section class="mt-8">
    <h2 class="mb-4 flex items-center gap-2.5 font-casual text-2xl tracking-[1px]">
      <i class="fa-solid fa-star text-lg text-peach"></i> <ResourceString for="RatingsReviews" />
    </h2>
    <div
      class="grid place-items-center gap-3 rounded-3xl border-2 border-dashed border-bone-dark px-4 py-16 text-center text-onyx-light"
    >
      <i class="fa-regular fa-star text-4xl opacity-50"></i>
      <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
      <p class="text-base"><ResourceString for="ReviewsComingSoon" /></p>
    </div>
  </section>
</template>
