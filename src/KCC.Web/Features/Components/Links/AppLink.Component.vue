<script setup lang="ts">
  export interface AppLinkProps {
    href: string
  }

  export interface AppLinkSlots {
    default: () => void
  }

  const { href } = defineProps<AppLinkProps>()
  defineSlots<AppLinkSlots>()
</script>

<template>
  <a :href="href">
    <slot />
  </a>
</template>
