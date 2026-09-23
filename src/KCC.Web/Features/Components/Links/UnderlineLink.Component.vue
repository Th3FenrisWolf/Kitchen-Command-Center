<!-- #region UnderlineLink Component -->
<script lang="ts">
  import AppLink, { type AppLinkProps, type AppLinkSlots } from '~/Components/Links/AppLink.Component.vue'

  /**
   * AppLink styled as an ink link with a hair-strong underline.
   */
  export default {
    name: 'UnderlineLink',
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { href } = defineProps<AppLinkProps>()
  defineSlots<AppLinkSlots>()
</script>

<template>
  <AppLink :href class="kcc-link">
    <slot />
  </AppLink>
</template>
