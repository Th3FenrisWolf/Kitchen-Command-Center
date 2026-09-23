<!-- #region ComingSoonSection Component Properties -->
<script lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import ComingSoonBadge from './ComingSoonBadge.vue'

  /**
   * Dashed-outline placeholder standing in for a feature that has not shipped yet.
   */
  export default {
    name: 'ComingSoonSection',
  }

  export interface ComingSoonSectionProps {
    /**
     * Resource string key, resolved against the Shared prefix.
     */
    textKey: string
    /**
     * Font Awesome classes, e.g. 'fa-duotone fa-utensils'.
     */
    icon?: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
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
