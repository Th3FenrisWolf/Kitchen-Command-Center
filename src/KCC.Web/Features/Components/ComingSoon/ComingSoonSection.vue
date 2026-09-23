<!-- #region ComingSoonSection Component Properties -->
<script lang="ts">
  import { ResourceString } from '~/Components/ResourceStrings'
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'

  /**
   * Lavender-washed sheet standing in for a feature that has not shipped yet.
   */
  export default {
    name: 'ComingSoonSection',
  }

  export interface ComingSoonSectionProps {
    /**
     * Resource string key for the body text, resolved under the same prefix as the label.
     */
    textKey: string

    /**
     * Neighbours never share a tear; pass a different preset when two sections sit together.
     * @default 3
     */
    tear?: Tear

    /**
     * Resolve the label and `textKey` against the Shared prefix. A page whose copy lives under its
     * own prefix instead — no matching `Shared.*` keys exist — opts out once for the whole section.
     * @default true
     */
    shared?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { textKey, tear = 3, shared = true } = defineProps<ComingSoonSectionProps>()
</script>

<template>
  <KccSheet
    wash="lavender"
    :at="{ x: '85%', y: '15%', w: '50%', h: '55%' }"
    icon="fa-duotone fa-hourglass-half"
    :tear="tear"
  >
    <template #label><ResourceString :shared="shared" for="ComingSoon" /></template>
    <ResourceString :shared="shared" :for="textKey" as="p" class="kcc-body" />
  </KccSheet>
</template>
