<!-- #region ResourceString Component Properties -->
<script lang="ts">
  import { computed, inject } from 'vue'
  import { resourceStringsKey } from '~/Components/ResourceStrings/UseResourceStrings'

  /**
   * Renders one localized string, falling back to its own key when the string is missing.
   */
  export default {
    name: 'ResourceString',
  }

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
</script>
<!-- #endregion -->

<script setup lang="ts">
  const props = defineProps<ResourceStringProps>()
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
