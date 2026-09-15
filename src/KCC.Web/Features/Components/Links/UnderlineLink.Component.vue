<!-- #region UnderlineLink Component -->
<script lang="ts">
  import AppLink, { type AppLinkProps, type AppLinkSlots } from '~/Components/Links/AppLink.Component.vue'

  /**
   * AppLink whose underline wipes out from the center on hover and focus.
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
  <AppLink :href class="group relative my-1 inline-block focus:outline-0">
    <span>
      <slot />
    </span>
    <span
      :class="[
        'absolute right-0 bottom-0 left-1/2 h-1 w-0 rounded-full bg-current transition-all group-hover:left-0 group-hover:w-full group-focus:left-0 group-focus:w-full',
      ]"
    />
  </AppLink>
</template>
