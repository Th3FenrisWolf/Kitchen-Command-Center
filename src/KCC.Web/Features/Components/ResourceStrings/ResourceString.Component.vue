<script setup lang="ts">
  import { computed, inject } from 'vue'
  import { resourceStringsKey } from '~/Components/ResourceStrings/UseResourceStrings'

  const props = defineProps<{
    for: string
    as?: string
    shared?: boolean
  }>()
  const ctx = inject(resourceStringsKey, { strings: {}, prefix: undefined })

  const isPreview = inject('isPreview', false)

  const resolvedPrefix = computed(() => (props.shared ? 'Shared' : ctx.prefix))
  const resolvedKey = computed(() => (resolvedPrefix.value ? `${resolvedPrefix.value}.${props.for}` : props.for))
  const resolvedValue = computed(() => ctx.strings[resolvedKey.value] ?? resolvedKey.value)
</script>

<template>
  <component
    :is="props.as || 'span'"
    :class="isPreview ? 'kcc-rs-editable' : undefined"
    :data-resource-key="isPreview ? resolvedKey : undefined"
  >
    {{ resolvedValue }}
  </component>
</template>
