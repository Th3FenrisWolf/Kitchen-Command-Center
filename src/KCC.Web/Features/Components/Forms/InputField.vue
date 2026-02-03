<script setup lang="ts">
defineOptions({
  inheritAttrs: false,
})

const model = defineModel({
  required: true,
})
</script>

<template>
  <input
    class="h-12 w-full rounded-2xl bg-bone-dark px-4 py-2 placeholder:text-onyx-light"
    v-bind="$attrs"
    v-model="model"
  />
</template>
