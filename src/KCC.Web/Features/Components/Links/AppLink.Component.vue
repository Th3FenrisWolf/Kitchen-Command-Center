<script setup lang="ts">
  const { href } = defineProps<{ href: string }>()
</script>

<template>
  <a :href="href.stripTilde()">
    <slot />
  </a>
</template>
