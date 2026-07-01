<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import ComingSoonBadge from './ComingSoonBadge.vue'

  defineProps<{
    textKey: string
    icon?: string
  }>()
</script>

<template>
  <section
    class="mt-2 grid place-items-center gap-3 rounded-3xl border-2 border-dashed border-bone-dark px-4 py-16 text-center text-onyx-light"
  >
    <i v-if="icon" :class="icon" aria-hidden="true"></i>
    <ComingSoonBadge />
    <ResourceString shared :for="textKey" as="p" class="text-base" />
  </section>
</template>
