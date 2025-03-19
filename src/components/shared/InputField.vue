<script setup lang="ts">
import type { InputHTMLAttributes } from 'vue'

const { required, readonly, type, autocomplete, placeholder } = defineProps<{
  required?: InputHTMLAttributes['required']
  readonly?: InputHTMLAttributes['readonly']
  type?: InputHTMLAttributes['type']
  autocomplete?: InputHTMLAttributes['autocomplete']
  placeholder?: InputHTMLAttributes['placeholder']
}>()

const model = defineModel<string>({
  required: true,
})
</script>

<template>
  <input
    class="bg-bone-dark placeholder:text-onyx-light rounded-2xl px-4 py-2"
    v-model="model"
    :required
    :readonly
    :type
    :autocomplete
    :placeholder
  />
</template>
