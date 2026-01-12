<script setup lang="ts">
defineProps<{
  headerContent: string
  bodyContent: string
  footerContent: string
}>()
</script>

<template>
  <component :is="{ template: headerContent }" />

  <main role="main" class="content-grid auto-rows-min">
    <component :is="{ template: bodyContent }" />
  </main>

  <component :is="{ template: footerContent }" />
</template>

<style>
html {
  background-color: var(--color-bone);
}
</style>
