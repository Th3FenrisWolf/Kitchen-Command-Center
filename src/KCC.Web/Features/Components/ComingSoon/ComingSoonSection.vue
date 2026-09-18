<!-- #region ComingSoonSection Component Properties -->
<script lang="ts">
  import { computed, inject } from 'vue'
  import { resourceStringsKey } from '~/Components/ResourceStrings/UseResourceStrings'
  import { ResourceString } from '~/Components/ResourceStrings'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'

  /**
   * Lavender-washed sheet standing in for a feature that has not shipped yet.
   */
  export default {
    name: 'ComingSoonSection',
  }

  export interface ComingSoonSectionProps {
    /**
     * Resource string key, resolved against the Shared prefix.
     */
    textKey: string
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { textKey } = defineProps<ComingSoonSectionProps>()

  // Imperative lookup: KccSheet's `label` takes a plain string, not a slot, so the shared
  // "ComingSoon" string is resolved here the same way <ResourceString shared for="ComingSoon">
  // does internally — falling back to the raw key when the string hasn't been entered yet.
  const ctx = inject(resourceStringsKey, { strings: {}, prefix: undefined })
  const comingSoonLabel = computed(() => ctx.strings['Shared.ComingSoon'] ?? 'Shared.ComingSoon')
</script>

<template>
  <KccSheet
    wash="lavender"
    :at="{ x: '85%', y: '15%', w: '50%', h: '55%' }"
    :label="comingSoonLabel"
    icon="fa-duotone fa-hourglass-half"
    :tear="3"
  >
    <ResourceString shared :for="textKey" as="p" class="kcc-body" />
  </KccSheet>
</template>
