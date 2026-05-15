<script setup lang="ts">
  import { computed, inject } from 'vue'
  import { resourceStringsKey } from '~/Components/ResourceStrings/UseStrings'

  const props = defineProps<{
    k: string
  }>()

  const ctx = inject(resourceStringsKey, { strings: {}, prefix: undefined })

  const resolvedKey = computed(() => (ctx.prefix ? `${ctx.prefix}.${props.k}` : props.k))

  const displayValue = computed(() => {
    if (!resolvedKey.value) return props.k
    return ctx.strings[resolvedKey.value] ?? props.k
  })
</script>

<template>
  <span :class="resolvedKey ? 'kcc-rs-editable' : undefined" :data-resource-key="resolvedKey">{{ displayValue }}</span>
</template>
