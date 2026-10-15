<script setup lang="ts">
  import { computed } from 'vue'
  import { useResourceStrings } from '~/Components/ResourceStrings'
  import { recipeWizardKeys } from './recipeWizardKeys'
  import { fill } from './wizardText'
  import type { Mark, MarkCode, MarkKind } from './wizardValues'

  export interface ValueMarkProps {
    mark: Mark
  }

  const { mark } = defineProps<ValueMarkProps>()

  const rs = useResourceStrings()

  const KIND_KEYS: Record<MarkKind, string> = {
    estimate: recipeWizardKeys.MarkEstimate,
    unclear: recipeWizardKeys.MarkUnclear,
    written: recipeWizardKeys.MarkWritten,
    translated: recipeWizardKeys.MarkTranslated,
  }

  const REASON_KEYS: Record<MarkCode, string> = {
    shortened: recipeWizardKeys.MarkReasonShortened,
    outOfRange: recipeWizardKeys.MarkReasonOutOfRange,
    translated: recipeWizardKeys.MarkReasonTranslated,
  }

  const reason = computed(() => (mark.code ? fill(rs(REASON_KEYS[mark.code]), mark.arg ?? '') : mark.reason))
</script>

<template>
  <span class="flex items-start gap-x-2" data-testid="value-mark" :data-kind="mark.kind">
    <span class="kcc-badge my-px">{{ rs(KIND_KEYS[mark.kind]) }}</span>
    <span v-if="reason" class="kcc-body text-ink-soft">{{ reason }}</span>
  </span>
</template>
