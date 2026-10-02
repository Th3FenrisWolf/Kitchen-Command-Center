<script setup lang="ts">
  import { computed, useAttrs, type StyleValue } from 'vue'

  export interface InputFieldProps {
    icon?: string
  }

  defineOptions({
    // Attributes land on the <input> itself rather than being split across the wrapper — except
    // class/style, which style the pill (the label) a caller sees as this component's root.
    inheritAttrs: false,
  })

  const { icon } = defineProps<InputFieldProps>()

  const model = defineModel({
    required: true,
  })

  const attrs = useAttrs()
  const inputAttrs = computed(() => {
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
