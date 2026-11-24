<script setup lang="ts">
  import KccSheet, { type Tear } from '~/Components/Sheet/KccSheet.vue'
  import PartFailed from './PartFailed.vue'
  import { STEP_PAD } from './stepSheet'

  // A later step's own props arrive here too; none of them may land on the sheet as attributes.
  defineOptions({ inheritAttrs: false })

  defineProps<{ tear?: Tear; flip?: boolean; error?: Error }>()
</script>

<template>
  <KccSheet
    wash-key="wizard-pending"
    margin
    crisp
    wide
    :tear
    :flip
    :pad="STEP_PAD"
    :aria-busy="error ? undefined : 'true'"
    :data-later-pending="error ? undefined : ''"
  >
    <PartFailed v-if="error" />
  </KccSheet>
</template>
