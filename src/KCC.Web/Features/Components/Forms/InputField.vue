<!-- #region InputField Component -->
<script lang="ts">
  /**
   * Single-line text input carrying the app's form styling.
   */
  export default {
    name: 'InputField',
    // Attributes land on the <input> itself rather than being split across the wrapper — except
    // class/style, which style the pill (the label) a caller sees as this component's root.
    inheritAttrs: false,
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { computed, useAttrs, type StyleValue } from 'vue'

  const model = defineModel({
    required: true,
  })

  const attrs = useAttrs()
  const rootStyle = computed(() => attrs.style as StyleValue | undefined)
  const inputAttrs = computed(() => {
    const rest = { ...attrs }
    delete rest.class
    delete rest.style
    return rest
  })
</script>

<template>
  <label class="kcc-field kcc-field--noicon" :class="attrs.class" :style="rootStyle">
    <input v-bind="inputAttrs" v-model="model" />
  </label>
</template>
