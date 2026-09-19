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
     * Resource string key, resolved against the Shared prefix unless `sharedText` is false.
     */
    textKey: string

    /**
     * Neighbours never share a tear; pass a different preset when two sections sit together.
     * @default 3
     */
    tear?: Tear

    /**
     * Resolve `textKey` against the Shared prefix. A caller whose copy lives under its own page
     * prefix instead — no matching `Shared.*` key exists — sets this false; the `ComingSoon` label
     * itself stays Shared regardless.
     * @default true
     */
    sharedText?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { textKey, tear = 3, sharedText = true } = defineProps<ComingSoonSectionProps>()
</script>

<template>
  <KccSheet
    wash="lavender"
    :at="{ x: '85%', y: '15%', w: '50%', h: '55%' }"
    icon="fa-duotone fa-hourglass-half"
    :tear="tear"
  >
    <template #label><ResourceString shared for="ComingSoon" /></template>
    <ResourceString :shared="sharedText" :for="textKey" as="p" class="kcc-body" />
  </KccSheet>
</template>
