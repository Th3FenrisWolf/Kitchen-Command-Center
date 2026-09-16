<!-- #region InputField Component -->
<script lang="ts">
  /**
   * Single-line text input carrying the app's form styling.
   */
  export default {
    name: 'InputField',
    // Attributes land on the <input> itself rather than being split across the wrapper.
    inheritAttrs: false,
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const model = defineModel({
    required: true,
  })
</script>

<template>
  <input
    class="h-12 w-full rounded-2xl border border-edge-strong bg-paper-2 px-4 py-2 placeholder:text-ink-soft"
    v-bind="$attrs"
    v-model="model"
  />
</template>
