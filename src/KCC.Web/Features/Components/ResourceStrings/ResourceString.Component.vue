<script setup lang="ts">
  import { computed, inject } from 'vue'
  import { resourceStringsKey } from '~/Components/ResourceStrings/UseResourceStrings'

  export interface ResourceStringProps {
    /**
     * Unprefixed key; the page's provided prefix is prepended before lookup.
     */
    for: string
    /**
     * Element to render as.
     * @default 'span'
     */
    as?: string
    /**
     * Look the key up under the 'Shared' prefix instead of the page's own.
     */
    shared?: boolean
  }

  const props = defineProps<ResourceStringProps>()
  const ctx = inject(resourceStringsKey, { strings: {}, prefix: undefined })

  const resolvedPrefix = computed(() => (props.shared ? 'Shared' : ctx.prefix))
  const resolvedKey = computed(() => (resolvedPrefix.value ? `${resolvedPrefix.value}.${props.for}` : props.for))
  const resolvedValue = computed(() => ctx.strings[resolvedKey.value] ?? resolvedKey.value)
</script>

<template>
  <component :is="props.as || 'span'">
    {{ resolvedValue }}
  </component>
</template>
