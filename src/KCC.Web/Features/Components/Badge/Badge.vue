<!-- Badge Component Properties -->
<script lang="ts">
  import type { HTMLAttributes } from 'vue'

  /**
   * Badge is a small, rounded component that can be used to display a status or label.
   */
  export default {
    name: 'Badge',
  }

  export interface BadgeProps {
    /**
     * The CSS classes to apply to the badge
     * This can be used to change the background color, text color, etc.
     * @default 'bg-bone-dark text-onyx-light'
     */
    class?: HTMLAttributes['class']
  }

  export interface BadgeSlots {
    /**
     * The content to display in the badge
     */
    default: () => void
  }
</script>
<!-- End Badge Component Properties -->

<script setup lang="ts">
  const { class: className = 'bg-bone-dark text-onyx-light' } = defineProps<BadgeProps>()
  defineSlots<BadgeSlots>()
</script>

<template>
  <span :class="[`inline-flex items-center rounded-full px-2 py-1 text-sm font-bold`, className]">
    <slot />
  </span>
</template>
