<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'

  defineEmits<{ clear: [] }>()
</script>

<template>
  <div
    class="mt-6 grid place-items-center gap-3 rounded-3xl border-2 border-dashed border-bone-dark px-4 py-16 text-center text-onyx-light"
  >
    <i class="fa-solid fa-bowl-food text-4xl opacity-50"></i>
    <div class="font-casual text-2xl text-onyx"><ResourceString for="NoVariantsMatch" /></div>
    <p class="text-base"><ResourceString for="TryDifferentFilter" /></p>
    <button
      type="button"
      class="mt-1.5 cursor-pointer rounded-full border-2 border-onyx bg-transparent px-5 py-2 text-base font-bold text-onyx"
      @click="$emit('clear')"
    >
      <ResourceString for="ClearFilters" />
    </button>
  </div>
</template>
