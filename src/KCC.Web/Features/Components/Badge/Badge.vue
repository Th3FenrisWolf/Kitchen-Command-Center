<script setup lang="ts">
  export interface BadgeSlots {
    default: () => void
  }

  defineSlots<BadgeSlots>()
</script>

<template>
  <span class="kcc-badge">
    <slot />
  </span>
</template>
