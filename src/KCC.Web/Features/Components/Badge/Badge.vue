<!-- #region Badge Component Properties -->
<script lang="ts">
  /**
   * Hairline pill (`kcc-badge`) that displays a short label. One uniform look; no colour variants.
   */
  export default {
    name: 'Badge',
  }

  export interface BadgeSlots {
    default: () => void
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  defineSlots<BadgeSlots>()
</script>

<template>
  <span class="kcc-badge">
    <slot />
  </span>
</template>
