<script setup lang="ts">
const { id } = defineProps<{
  id: number
}>()
</script>

<template>
  <div>Welcome to Recipe Detail: {{ id }}</div>
</template>
