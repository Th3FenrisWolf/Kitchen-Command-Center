<script setup lang="ts">
  import AppLink, { type AppLinkProps, type AppLinkSlots } from '~/Components/Links/AppLink.Component.vue'

  const { href } = defineProps<AppLinkProps>()
  defineSlots<AppLinkSlots>()
</script>

<template>
  <AppLink :href class="kcc-link">
    <slot />
  </AppLink>
</template>
