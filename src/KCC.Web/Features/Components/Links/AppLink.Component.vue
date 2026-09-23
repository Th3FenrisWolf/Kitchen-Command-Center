<!-- #region AppLink Component Properties -->
<script lang="ts">
  /**
   * Anchor that resolves Kentico's tilde-relative paths before rendering.
   */
  export default {
    name: 'AppLink',
  }

  export interface AppLinkProps {
    /**
     * A leading `~/` is stripped on render, so content-tree paths can be passed through as-is.
     */
    href: string
  }

  export interface AppLinkSlots {
    default: () => void
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { href } = defineProps<AppLinkProps>()
  defineSlots<AppLinkSlots>()
</script>

<template>
  <a :href="href.stripTilde()">
    <slot />
  </a>
</template>
