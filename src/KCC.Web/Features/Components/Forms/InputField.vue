<script setup lang="ts">
const model = defineModel({
  required: true,
})
</script>

<template>
  <input class="h-12 rounded-2xl bg-bone-dark px-4 py-2 placeholder:text-onyx-light" v-model="model" />
</template>
