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
  <div class="mb-8 flex gap-2" role="progressbar" :aria-valuenow="current" aria-valuemin="1" :aria-valuemax="total">
    <div
      v-for="s in total"
      :key="s"
      class="h-2 flex-1 rounded-full transition-colors"
      :class="s <= current ? 'bg-marker' : 'bg-desk-2'"
    />
  </div>
</template>
