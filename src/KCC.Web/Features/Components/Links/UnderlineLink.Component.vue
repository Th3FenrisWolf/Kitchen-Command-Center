<script setup lang="ts">
  const { href } = defineProps<{ href: string }>()
  defineSlots<{ default: () => void }>()

  defineOptions({ serverOnly: true })
</script>

<template>
  <a :href class="kcc-link">
    <slot />
  </a>
</template>
