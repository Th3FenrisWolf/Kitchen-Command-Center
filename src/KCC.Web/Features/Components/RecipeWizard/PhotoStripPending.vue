<script setup lang="ts">
  import { computed, inject, onBeforeUnmount, watch } from 'vue'
  import KccSheet from '~/Components/Sheet/KccSheet.vue'
  import PartFailed from './PartFailed.vue'
  import { failedStripSheet, STEP_SHEET } from './stepFlip'
  import { STEP_PAD } from './stepSheet'

  // The strip's own props and test id arrive here too; a pending strip is not yet the strip.
  defineOptions({ inheritAttrs: false })

  const { error } = defineProps<{ error?: Error }>()

  const stepSheet = inject(STEP_SHEET, null)
  const sheet = computed(() => failedStripSheet(stepSheet?.step.value ?? 1))

  const tellWizard = (failed: boolean) => {
    if (stepSheet) stepSheet.stripFailed.value = failed
  }

  watch(() => !!error, tellWizard, { immediate: true })
  onBeforeUnmount(() => tellWizard(false))
</script>

<template>
  <KccSheet
    v-if="error"
    wash-key="wizard-photos-failed"
    margin
    crisp
    wide
    :tear="sheet.tear"
    :flip="sheet.flip"
    :pad="STEP_PAD"
    :class="$attrs.class"
  >
    <PartFailed />
  </KccSheet>
  <div v-else aria-busy="true" data-later-pending></div>
</template>
