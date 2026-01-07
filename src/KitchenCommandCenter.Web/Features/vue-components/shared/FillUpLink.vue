<script setup lang="ts">
const { linkTo } = defineProps<{ linkTo: string }>()
</script>

<template>
  <div class="my-1">
    <a :href="linkTo">
      <slot />
    </a>
  </div>
</template>
