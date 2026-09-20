<!-- #region WizardProgress Component Properties -->
<script lang="ts">
  /**
   * Step position for a multi-step form: numbered kick labels on the rule joined by a dashed hair
   * connector, the current step a marker pill. An ordered list with `aria-current="step"`, so
   * assistive tech gets the count from the list itself.
   */
  export default {
    name: 'WizardProgress',
  }

  export interface WizardProgressProps {
    /** 1-based index of the step being shown. */
    current: number

    total: number
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { current, total } = defineProps<WizardProgressProps>()
</script>

<template>
  <ol class="mb-6 flex items-center gap-2" role="list" aria-label="Steps">
    <li
      v-for="s in total"
      :key="s"
      :class="['flex items-center gap-2', s < total && 'flex-1']"
      :aria-current="s === current ? 'step' : undefined"
    >
      <span :class="['kcc-kick', s === current ? 'kcc-pill px-3' : s < current ? 'text-ink' : 'text-ink-soft']">
        <span aria-hidden="true">{{ String(s).padStart(2, '0') }}</span
        ><span class="sr-only">{{ s }}</span>
      </span>
      <span v-if="s < total" class="h-0 flex-1 border-t border-dashed border-hair-strong" aria-hidden="true"></span>
    </li>
  </ol>
</template>
