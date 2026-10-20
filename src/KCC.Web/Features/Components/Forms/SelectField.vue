<script setup lang="ts">
  import { computed, useAttrs, type StyleValue } from 'vue'

  export interface SelectFieldProps {
    error?: boolean
  }

  defineOptions({
    inheritAttrs: false,
  })

  const { error = false } = defineProps<SelectFieldProps>()

  const attrs = useAttrs()
  const selectAttrs = computed(() => {
    const { class: _class, style: _style, ...rest } = attrs
    return rest
  })
</script>

<template>
  <span :class="['kcc-field', { 'kcc-field--error': error }, 'w-full', attrs.class]" :style="attrs.style as StyleValue">
    <select
      v-bind="selectAttrs"
      :aria-invalid="error || (selectAttrs['aria-invalid'] as 'true' | 'false' | undefined) || undefined"
    >
      <slot />
    </select>
  </span>
</template>
