<script setup lang="ts">
import { computed } from 'vue'

const { href } = defineProps<{ href: string }>()

// Normalize URLs: convert ASP.NET ~/path format to /path
const normalizedHref = computed(() => {
  if (href?.startsWith('~/')) {
    return href.slice(1) // Remove the ~ prefix, keep the /
  }
  return href
})
</script>

<template>
  <a :href="normalizedHref">
    <slot />
  </a>
</template>
