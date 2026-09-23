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
    v-ink="{ kind: 'sheet', hatch: false }"
    class="sk-sheet sk-sheet--lg mt-2 grid place-items-center gap-4 text-center"
  >
    <span class="sk-wash" style="--c: var(--color-lavender)" aria-hidden="true"></span>
    <i v-if="icon" :class="['sk-ico', icon]"></i>
    <ComingSoonBadge />
    <ResourceString shared :for="textKey" as="p" class="sk-body" />
  </section>
</template>
