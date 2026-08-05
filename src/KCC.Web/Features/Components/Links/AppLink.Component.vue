<!-- #region Link Component Properties -->
<script lang="ts">
  export interface LinkComponentProps {
    /**
     * The URL the link should navigate to
     */
    href: string
  }

  export interface LinkComponentSlots {
    /**
     * The content to display in the link
     */
    default: () => void
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { href } = defineProps<LinkComponentProps>()
  defineSlots<LinkComponentSlots>()
</script>

<template>
  <a :href="href.stripTilde()">
    <slot />
  </a>
</template>
