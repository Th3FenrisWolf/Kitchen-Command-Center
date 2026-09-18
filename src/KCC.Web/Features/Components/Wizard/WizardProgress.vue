<!-- #region WizardProgress Component Properties -->
<script lang="ts">
  /**
   * Step position for a multi-step form. Segmented rather than a continuous bar: N segments say
   * "step 2 of 5", which a single fill cannot.
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
  <ol class="mb-8 flex items-center gap-2" aria-label="Steps">
    <li
      v-for="s in total"
      :key="s"
      class="flex flex-1 items-center gap-2"
      :aria-current="s === current ? 'step' : undefined"
    >
      <span
        :class="
          s === current
            ? 'kcc-kick rounded-md bg-marker px-3 text-marker-ink'
            : s < current
              ? 'kcc-kick text-ink'
              : 'kcc-kick text-ink-soft'
        "
        >{{ String(s).padStart(2, '0') }}</span
      >
      <span v-if="s < total" class="h-0 flex-1 border-t border-dashed border-hair-strong" aria-hidden="true"></span>
    </li>
  </ol>
</template>
