<script setup lang="ts">
const { linkTo } = defineProps<{ linkTo: string }>()
</script>

<template>
  <div class="my-1">
    <RouterLink :to="linkTo">
      <slot />
    </RouterLink>
  </div>
</template>
