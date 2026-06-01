<script setup lang="ts">
  import { computed, inject } from 'vue'
  import { resourceStringsKey } from '~/Components/ResourceStrings/UseStrings'

  const props = defineProps<{
    for: string
    as?: string
  }>()
  const ctx = inject(resourceStringsKey, { strings: {}, prefix: undefined })

  const isPreview = inject('isPreview', false)

  const resolvedKey = computed(() => (ctx.prefix ? `${ctx.prefix}.${props.for}` : props.for))
  const resolvedValue = computed(() => ctx.strings[resolvedKey.value] ?? resolvedKey.value)
</script>

<template>
  <component :is="props.as || 'span'" v-if="isPreview" class="kcc-rs-editable" :data-resource-key="resolvedKey">
    {{ resolvedValue }}
  </component>
  <component :is="props.as || 'span'" v-else>
    {{ resolvedValue }}
  </component>
</template>
