<!-- #region Coming Soon Section Component Properties -->
<script lang="ts">
  export interface ComingSoonSectionProps {
    /**
     * The key for the text to display in the coming soon section
     */
    textKey: string
    /**
     * The icon to display in the coming soon section
     */
    icon?: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import ComingSoonBadge from './ComingSoonBadge.vue'

  const { textKey, icon } = defineProps<ComingSoonSectionProps>()
</script>

<template>
  <section
    class="mt-2 grid place-items-center gap-4 rounded-3xl border-2 border-dashed border-bone-dark px-4 py-16 text-center text-onyx-light"
  >
    <i v-if="icon" :class="icon"></i>
    <ComingSoonBadge />
    <ResourceString shared :for="textKey" as="p" class="text-base" />
  </section>
</template>
