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
    suggested: recipeWizardKeys.MarkSuggested,
    calculated: recipeWizardKeys.MarkCalculated,
  }

  const REASON_KEYS: Record<MarkCode, string> = {
    shortened: recipeWizardKeys.MarkReasonShortened,
    outOfRange: recipeWizardKeys.MarkReasonOutOfRange,
    translated: recipeWizardKeys.MarkReasonTranslated,
    printedAs: recipeWizardKeys.MarkReasonPrintedAs,
  }

  const needsArg = (code: MarkCode) => code === 'printedAs' || code === 'translated'

  interface Reason {
    before: string
    amount: string | null
    after: string
  }

  const holdsNumber = (text: string) => /\d/.test(text)

  const said = (text: string): Reason => ({ before: text, amount: null, after: '' })

  const reason = computed((): Reason | null => {
    if (!mark.code || (needsArg(mark.code) && !mark.arg)) {
      return mark.reason ? said(mark.reason) : null
    }
    const template = rs(REASON_KEYS[mark.code])
    const arg = mark.arg ?? ''
    const [before = '', ...rest] = template.split('{0}')
    return holdsNumber(arg) && rest.length > 0 ? { before, amount: arg, after: rest.join(arg) } : said(fill(template, arg))
  })
</script>

<template>
  <span class="flex items-start gap-x-2" data-testid="value-mark" :data-kind="mark.kind">
    <span class="kcc-badge">{{ rs(KIND_KEYS[mark.kind]) }}</span>
    <span v-if="reason" class="kcc-body text-ink-soft"
      >{{ reason.before }}<span v-if="reason.amount" class="kcc-num">{{ reason.amount }}</span
      >{{ reason.after }}</span
    >
  </span>
</template>
