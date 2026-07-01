<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import Badge from '~/Components/Badge/Badge.vue'
</script>

<template>
  <section class="mt-8">
    <h2 class="mb-4 flex items-center gap-2.5 font-casual text-2xl tracking-[1px]">
      <i class="fa-solid fa-lightbulb text-lg text-yellow"></i> <ResourceString for="CookNotes" />
    </h2>
    <div
      class="grid place-items-center gap-3 rounded-3xl border-2 border-dashed border-bone-dark px-4 py-12 text-center text-onyx-light"
    >
      <Badge color="muted"><ResourceString for="ComingSoon" /></Badge>
      <p class="text-base"><ResourceString for="CookNotesComingSoon" /></p>
    </div>
  </section>
</template>
