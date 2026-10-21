<script setup lang="ts">
  import { computed, useAttrs, type StyleValue } from 'vue'

  export interface InputFieldProps {
    inputClass?: string
    error?: boolean
  }

  defineOptions({
    // Attributes land on the <input> itself rather than being split across the wrapper — except
    // class/style, which style the field (the label) a caller sees as this component's root.
    inheritAttrs: false,
  })

  const { inputClass, error = false } = defineProps<InputFieldProps>()

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
  <!-- `w-full` because the root is the field, not the input: a caller with `items-start` would otherwise
       shrink-wrap it. -->
  <label :class="['kcc-field', { 'kcc-field--error': error }, 'w-full', attrs.class]" :style="attrs.style as StyleValue">
    <input
      v-bind="inputAttrs"
      v-model="model"
      :class="inputClass"
      :aria-invalid="error || (inputAttrs['aria-invalid'] as 'true' | 'false' | undefined) || undefined"
    />
  </label>
</template>
