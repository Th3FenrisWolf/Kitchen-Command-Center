<!-- #region Badge Component Properties -->
<script lang="ts">
  import type { HTMLAttributes } from 'vue'

  /**
   * Small rounded pill that displays a status or label.
   */
  export default {
    name: 'Badge',
  }

  export interface BadgeProps {
    /**
     * @default 'bg-desk-2 text-ink-soft'
     */
    class?: HTMLAttributes['class']
  }

  export interface BadgeSlots {
    default: () => void
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { class: className = 'bg-desk-2 text-ink-soft' } = defineProps<BadgeProps>()
  defineSlots<BadgeSlots>()
</script>

<template>
  <span :class="['inline-flex items-center rounded-full px-2 py-1 text-sm font-bold', className]">
    <slot />
  </span>
</template>
