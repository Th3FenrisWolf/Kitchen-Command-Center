<!-- #region InputField Component Properties -->
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

  export interface InputFieldProps {
    /**
     * Font Awesome classes for a leading icon inside the pill, e.g. 'fa-duotone fa-magnifying-glass'.
     * Omit it and the pill is a single column (`kcc-field--noicon`); the kit colours the glyph itself.
     */
    icon?: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { computed, useAttrs, type StyleValue } from 'vue'

  const { icon } = defineProps<InputFieldProps>()

  const model = defineModel({
    required: true,
  })

  const attrs = useAttrs()
  const inputAttrs = computed(() => {
    // class and style dress the pill, so they stay on the root label; everything else is the input's.
    const { class: _class, style: _style, ...rest } = attrs
    return rest
  })
</script>

<template>
  <!-- `w-full` because the root is the pill, not the input: a caller with `items-start` would otherwise
       shrink-wrap it. -->
  <label :class="['kcc-field', { 'kcc-field--noicon': !icon }, 'w-full', attrs.class]" :style="attrs.style as StyleValue">
    <i v-if="icon" :class="icon" aria-hidden="true"></i>
    <input v-bind="inputAttrs" v-model="model" />
  </label>
</template>
