<script setup lang="ts">
const model = defineModel({
  required: true,
})
</script>

<template>
  <input
    class="bg-bone-dark placeholder:text-onyx-light h-12 rounded-2xl px-4 py-2"
    v-model="model"
  />
</template>
