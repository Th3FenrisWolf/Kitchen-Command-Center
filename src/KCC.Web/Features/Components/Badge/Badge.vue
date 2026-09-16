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
  <span :class="['sk-lbl inline-flex items-center rounded-full bg-paper-2 px-3 py-1 opacity-100', className]">
    <slot />
  </span>
</template>
