<!-- #region Field Component Properties -->
<script lang="ts">
  /**
   * Label, control, hint and error for one form field. The control goes in the slot and takes
   * `:id="controlId"`; the slot exposes `describedby` for the control's `aria-describedby`.
   */
  export default {
    name: 'Field',
  }

  export interface FieldProps {
    label: string

    /** The control's id; also keys the hint and error ids. */
    controlId: string

    hint?: string

    error?: string

    required?: boolean
  }
</script>
<!-- #endregion -->

<script setup lang="ts">
  const { label, controlId, hint, error, required = false } = defineProps<FieldProps>()
</script>

<template>
  <div class="sk-field flex flex-col gap-2">
    <label :for="controlId" class="sk-lbl"> {{ label }}<span v-if="required" aria-hidden="true"> *</span> </label>
    <slot :describedby="error ? `${controlId}-error` : hint ? `${controlId}-hint` : undefined" />
    <p v-if="error" :id="`${controlId}-error`" class="text-sm text-danger-ink" role="alert">{{ error }}</p>
    <p v-else-if="hint" :id="`${controlId}-hint`" class="text-sm text-ink-soft">{{ hint }}</p>
  </div>
</template>
